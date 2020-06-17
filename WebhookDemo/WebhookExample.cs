using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json;
using PX.Common;
using PX.Data;
using PX.Data.Webhooks;
using PX.Objects;
using PX.Objects.IN;

namespace WebhookExample
{
	public class Notification
	{
		public List<dynamic> Inserted { get; set; }
		public List<dynamic> Deleted { get; set; }
		public List<dynamic> Updated { get; set; }
		public string Query { get; set; }
		public string CompanyId { get; set; }
		public string Id { get; set; }
		public ulong TimeStamp { get; set; }
		public dynamic AdditionalInfo { get; set; }
	}

	public class NonStockItemWebhookHandler : IWebhookHandler
	{
		public async Task<System.Web.Http.IHttpActionResult> ProcessRequestAsync(
		  HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var ok = new OkResult(request);

			using (var scope = GetAdminScope())
			{

				try
				{
					// Request custom authorization header example
					var secret = string.Empty;

					if (request.Headers.TryGetValues("CustomAuthorization", out IEnumerable<string> headerValues))
					{
						secret = headerValues.FirstOrDefault();
					}

					// If secret does not match we reject the notification
					if (secret != "secretValue") return new StatusCodeResult(System.Net.HttpStatusCode.Unauthorized, request);

					// Deserialize JSON into our Notification class
					var notification = JsonConvert.DeserializeObject<Notification>(await request.Content.ReadAsStringAsync());

					// No changes to make, lets exit
					if (notification == null || 
						(notification?.Inserted?.Count ?? 0) < 1 && 
						(notification?.Deleted?.Count ?? 0) < 1) return ok;

					// Generate updated item list (if item appears in both it is updated)
					notification.Updated = (from inserted in notification.Inserted
										   join deleted in notification.Deleted on inserted.InventoryID equals deleted.InventoryID
											select inserted).ToList();

					// Remove updated from deleted
					notification.Deleted = (notification.Deleted.Where(deleted => !notification.Updated.Any(updated => updated.InventoryID == deleted.InventoryID))).ToList();

					// We will use this Graph to insert our new item
					var graph = PXGraph.CreateInstance<NonStockItemMaint>();

					foreach (var item in notification.Inserted)
					{
						// Only Non-stock items
						if (item.Type.ToString() != "Non-Stock Item") continue;

						// We set No
						string itemType = "N";

						string inventoryID = item.InventoryID.ToString().Trim();
						string classCD = item.ItemClass.ToString().Trim();
						InventoryItem newItem = graph.Item.Search<InventoryItem.inventoryCD>(inventoryID);
						INItemClass itemClass = null;


						if (!string.IsNullOrEmpty(classCD))
						{
							try
							{
								itemClass = graph.ItemClass.Select(classCD);
							}
							catch
							{
								itemClass = null;
							}
							
						}						

						// If we find the item we skip it
						if (newItem != null)
						{
							graph.Item.Current = newItem;
							// Set values
							graph.Item.Cache.SetValueExt(newItem, "Descr", item.Description.ToString());
							graph.Item.Cache.SetValueExt(newItem, "ItemType", itemType);
							graph.Item.Cache.SetValueExt(newItem, "PostClassID", item.PostingClass.ToString());
							graph.Item.Cache.SetValueExt(newItem, "TaxCategoryID", item.TaxCategory.ToString());
							graph.Item.Cache.SetValueExt(newItem, "NonStockReceipt", Convert.ToBoolean(item.RequireReceipt));
							graph.Item.Cache.SetValueExt(newItem, "NonStockShip", Convert.ToBoolean(item.RequireShipment));
							graph.Item.Cache.SetValueExt(newItem, "BaseUnit", item.BaseUnit.ToString());
							graph.Item.Cache.SetValueExt(newItem, "PurchaseUnit", item.PurchaseUnit.ToString());
							graph.Item.Cache.SetValueExt(newItem, "SalesUnit", item.SalesUnit.ToString());

							graph.Item.Cache.SetValueExt(newItem, "ItemClassID", itemClass?.ItemClassID);

							// Simple way for us to track automatic inserts
							graph.Item.Cache.SetValueExt(newItem, "NoteText", "Updated by Webhook");
						}
						else
						{
							newItem = graph.Item.Insert(new InventoryItem()
							{
								InventoryCD = inventoryID,
								Descr = item.Description.ToString(),
								ItemType = itemType,
								PostClassID = item.PostingClass.ToString(),
								ItemClassID = itemClass?.ItemClassID,
								TaxCategoryID = item.TaxCategory.ToString(),
								NonStockReceipt = Convert.ToBoolean(item.RequireReceipt),
								NonStockShip = Convert.ToBoolean(item.RequireShipment),
								BaseUnit = item.BaseUnit.ToString(),
								PurchaseUnit = item.PurchaseUnit.ToString(),
								SalesUnit = item.SalesUnit.ToString()
							});

							// Simple way for us to track automatic inserts
							graph.Item.Cache.SetValueExt(newItem, "NoteText", "Created by Webhook");
						}
					}

					foreach (var item in notification.Deleted)
					{
						string id = item.InventoryID.ToString().Trim();
						InventoryItem deletedItem = graph.Item.Search<InventoryItem.inventoryCD>(id);

						// If we dont find the item we go to next one
						if (deletedItem == null) continue;

						deletedItem = graph.Item.Delete(deletedItem);
					}

					graph.Actions.PressSave();
				} catch (Exception ex)
				{
					var failed = new ExceptionResult(ex, false, new DefaultContentNegotiator(), request, new[] { new JsonMediaTypeFormatter() });

					return failed;
				}

			}

			return ok;
		}

		private IDisposable GetAdminScope()
		{
			var userName = "admin";
			if (PXDatabase.Companies.Length > 0)
			{
				var company = PXAccess.GetCompanyName();
				if (string.IsNullOrEmpty(company))
				{
					company = PXDatabase.Companies[0];
				}
				userName = userName + "@" + company;
			}
			return new PXLoginScope(userName);
		}
	}
}
