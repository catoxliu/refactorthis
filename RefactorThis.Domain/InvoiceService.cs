using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
	public class InvoiceService
	{
		private readonly InvoiceRepository _invoiceRepository;

		public InvoiceService( InvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}

		public string ProcessPayment( Payment payment )
		{
			var inv = _invoiceRepository.GetInvoice( payment.Reference );

			if ( inv == null )
			{
				throw new InvalidOperationException( "There is no invoice matching this payment" );
			}

			if ( inv.Amount == 0 )
			{
				if ( inv.Payments == null || !inv.Payments.Any( ) )
				{
					SaveInvoice(inv);
					return "no payment needed";
				}
				else
				{
					throw new InvalidOperationException( "The invoice is in an invalid state, it has an amount of 0 and it has payments." );
				}
			}

			bool isIncludeTax = false;

			if ( inv.Payments != null && inv.Payments.Any( ) )
			{
				if ( inv.Payments.Sum( x => x.Amount ) != 0 && inv.Amount == inv.Payments.Sum( x => x.Amount ) )
				{
					SaveInvoice(inv);
					return "invoice was already fully paid";
				}
				else if ( inv.Payments.Sum( x => x.Amount ) != 0 && payment.Amount > ( inv.Amount - inv.AmountPaid ) )
				{
					SaveInvoice(inv);
					return "the payment is greater than the partial amount remaining";
				}
				
				if ( ( inv.Amount - inv.AmountPaid ) == payment.Amount )
				{
					switch ( inv.Type )
					{
						case InvoiceType.Standard:
							isIncludeTax = false;
							break;
						case InvoiceType.Commercial:
							isIncludeTax = true;
							break;
						default:
							throw new ArgumentOutOfRangeException( );
					}
					ProcessInvoice( inv, payment, isIncludeTax );
					SaveInvoice(inv);
					return "final partial payment received, invoice is now fully paid";
					
				}

				switch ( inv.Type )
				{
					case InvoiceType.Standard:
						isIncludeTax = false;
						break;
					case InvoiceType.Commercial:
						isIncludeTax = true;
						break;
					default:
						throw new ArgumentOutOfRangeException( );
				}
				ProcessInvoice( inv, payment, isIncludeTax );
				SaveInvoice(inv);
				return "another partial payment received, still not fully paid";
			}

			if ( payment.Amount > inv.Amount )
			{
				SaveInvoice(inv);
				return "the payment is greater than the invoice amount";
			}
			else if ( inv.Amount == payment.Amount ) // fully paid invoice
			{
				switch ( inv.Type )
				{
					case InvoiceType.Standard:
						isIncludeTax = true;
						break;
					case InvoiceType.Commercial:
						isIncludeTax = true;
						break;
					default:
						throw new ArgumentOutOfRangeException( );
				}
				ProcessInvoice( inv, payment, isIncludeTax );
				SaveInvoice(inv);
				return "invoice is now fully paid";
			}

			// Partially paid invoice
			switch ( inv.Type )
			{
				case InvoiceType.Standard:
					isIncludeTax = true;
					break;
				case InvoiceType.Commercial:
					isIncludeTax = true;
					break;
				default:
					throw new ArgumentOutOfRangeException( );
			}
			ProcessInvoice( inv, payment, isIncludeTax );
			SaveInvoice(inv);
			return "invoice is now partially paid";
		}

		private void SaveInvoice( Invoice invoice )
		{
			invoice.Save();
		}

		private void ProcessInvoice ( Invoice invoice, Payment payment, bool isIncludeTax )
		{
			invoice.AmountPaid += payment.Amount;
			if ( isIncludeTax )
			{
				invoice.TaxAmount += payment.Amount * 0.14m;
			}
			invoice.Payments.Add( payment );
		}
	}
}