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

		private enum PaymentStatus
		{
			FirstPayment,
			PartialPayment,
		}
		
		// TODO:
		//  I don't know why the invoice needs to be saved even though there's nothing changed in this process.
		//  Let's assume there's no need to save the invoice if we don't call ProcessPaymentInternal (no changes to the invoice),
		//  Then we can have a PaymentValidator function here to put all the exceptions in that function to just return messages or throw errors.

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

			var responseMessage = string.Empty;

			// Already partially paid invoice
			if ( inv.Payments != null && inv.Payments.Any( ) )
			{
				if ( inv.Payments.Sum( x => x.Amount ) != 0 && inv.Amount == inv.Payments.Sum( x => x.Amount ) )
				{
					SaveInvoice(inv);
					return "invoice was already fully paid";
				}
				
				if ( inv.Payments.Sum( x => x.Amount ) != 0 && payment.Amount > ( inv.Amount - inv.AmountPaid ) )
				{
					SaveInvoice(inv);
					return "the payment is greater than the partial amount remaining";
				}

				if ( ( inv.Amount - inv.AmountPaid ) == payment.Amount )
				{	
					responseMessage = "final partial payment received, invoice is now fully paid";	
				}
				else 
				{
					responseMessage = "another partial payment received, still not fully paid";
				}
                ProcessPaymentInternal( inv, payment, PaymentStatus.PartialPayment );
				return responseMessage;
			}

			if ( payment.Amount > inv.Amount )
			{
				SaveInvoice(inv);
				return "the payment is greater than the invoice amount";
			}

			if ( inv.Amount == payment.Amount )
			{
				responseMessage = "invoice is now fully paid";
			}
			else
			{
				responseMessage = "invoice is now partially paid";
			}
			
            ProcessPaymentInternal( inv, payment, PaymentStatus.FirstPayment );
			return responseMessage;
		}

		private void ProcessPaymentInternal( Invoice invoice, Payment payment, PaymentStatus paymentStatus )
		{
			bool isIncludeTax = ProcessTax( invoice.Type, paymentStatus );
			ProcessInvoice( invoice, payment, isIncludeTax );
			SaveInvoice(invoice);
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

		private bool ProcessTax (InvoiceType invoiceType, PaymentStatus paymentStatus)
		{
			switch ( invoiceType )
			{
				case InvoiceType.Standard:
					// for standard invoice, tax is included only for the first payment
					if ( paymentStatus == PaymentStatus.FirstPayment )
					{
						return true;
					}
					return false;
				case InvoiceType.Commercial:
					// commercial invoice always includes tax
					return true;
				default:
					throw new ArgumentOutOfRangeException( );
			}
		}
	}
}