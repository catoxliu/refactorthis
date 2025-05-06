using System;
using System.Collections.Generic;
using NUnit.Framework;
using RefactorThis.Persistence;

namespace RefactorThis.Domain.Tests
{
	[TestFixture]
	public class InvoicePaymentProcessorTests
	{
		[Test]
		public void ProcessPayment_Should_ThrowException_When_NoInoiceFoundForPaymentReference( )
		{
			var repo = new InvoiceRepository( );

			Invoice invoice = null;
			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( );
			var failureMessage = "";

			try
			{
				var result = paymentProcessor.ProcessPayment( payment );
			}
			catch ( InvalidOperationException e )
			{
				failureMessage = e.Message;
			}

			Assert.AreEqual( "There is no invoice matching this payment", failureMessage );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_NoPaymentNeeded( )
		{
			var repo = new InvoiceRepository( );

			var invoice = new Invoice( repo )
			{
				Amount = 0,
				AmountPaid = 0,
				Payments = null
			};

			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( );

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "no payment needed", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_InvoiceAlreadyFullyPaid( )
		{
			var repo = new InvoiceRepository( );

			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 10,
				Payments = new List<Payment>
				{
					new Payment
					{
						Amount = 10
					}
				}
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( );

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "invoice was already fully paid", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 5,
				Payments = new List<Payment>
				{
					new Payment
					{
						Amount = 5
					}
				}
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 6
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "the payment is greater than the partial amount remaining", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 5,
				AmountPaid = 0,
				Payments = new List<Payment>( )
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 6
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "the payment is greater than the invoice amount", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFullyPaidMessage_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 5,
				Payments = new List<Payment>
				{
					new Payment
					{
						Amount = 5
					}
				}
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 5
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "final partial payment received, invoice is now fully paid", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnFullyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidEqualsInvoiceAmount( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 0,
				Payments = new List<Payment>( ) { new Payment( ) { Amount = 10 } }
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 10
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "invoice was already fully paid", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 5,
				Payments = new List<Payment>
				{
					new Payment
					{
						Amount = 5
					}
				}
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 1
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "another partial payment received, still not fully paid", result );
		}

		[Test]
		public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount( )
		{
			var repo = new InvoiceRepository( );
			var invoice = new Invoice( repo )
			{
				Amount = 10,
				AmountPaid = 0,
				Payments = new List<Payment>( )
			};
			repo.Add( invoice );

			var paymentProcessor = new InvoiceService( repo );

			var payment = new Payment( )
			{
				Amount = 1
			};

			var result = paymentProcessor.ProcessPayment( payment );

			Assert.AreEqual( "invoice is now partially paid", result );
		}

		[Test]
		public void ProcessPayment_Should_ThrowException_When_InvoiceHasZeroAmountWithExistingPayments()
		{
			var repo = new InvoiceRepository();
			var invoice = new Invoice(repo)
			{
				Amount = 0,
				AmountPaid = 5,
				Payments = new List<Payment> { new Payment { Amount = 5 } }
			};
			repo.Add(invoice);

			var paymentProcessor = new InvoiceService(repo);
			var payment = new Payment();

			Assert.Throws<InvalidOperationException>(() => paymentProcessor.ProcessPayment(payment),
				"The invoice is in an invalid state, it has an amount of 0 and it has payments.");
		}

		[Test]
		public void ProcessPayment_Should_AddTaxAmount_When_FirstPaymentOnStandardInvoice()
		{
			var repo = new InvoiceRepository();
			var invoice = new Invoice(repo)
			{
				Amount = 100,
				AmountPaid = 0,
				Payments = new List<Payment>(),
				Type = InvoiceType.Standard
			};
			repo.Add(invoice);

			var paymentProcessor = new InvoiceService(repo);
			var payment = new Payment { Amount = 50, };

			paymentProcessor.ProcessPayment(payment);

			// Tax should be 14% of payment amount
			Assert.AreEqual(7.0m, invoice.TaxAmount);
		}

		[Test]
		public void ProcessPayment_Should_NotAddTaxAmount_When_PartialPaymentOnStandardInvoice()
		{
			var repo = new InvoiceRepository();
			var invoice = new Invoice(repo)
			{
				Amount = 100,
				AmountPaid = 50,
				TaxAmount = 7.0m, // Assuming 14% tax on first payment of 50
				Payments = new List<Payment> { new Payment { Amount = 50 } },
				Type = InvoiceType.Standard
			};
			repo.Add(invoice);

			var paymentProcessor = new InvoiceService(repo);
			var payment = new Payment { Amount = 25 };

			paymentProcessor.ProcessPayment(payment);

			// Tax should remain the same
			Assert.AreEqual(7.0m, invoice.TaxAmount);
		}

		[Test]
		public void ProcessPayment_Should_AlwaysAddTaxAmount_When_CommercialInvoice()
		{
			var repo = new InvoiceRepository();
			var invoice = new Invoice(repo)
			{
				Amount = 100,
				AmountPaid = 50,
				TaxAmount = 7.0m,
				Payments = new List<Payment> { new Payment { Amount = 50 } },
				Type = InvoiceType.Commercial
			};
			repo.Add(invoice);

			var paymentProcessor = new InvoiceService(repo);
			var payment = new Payment { Amount = 25 };

			paymentProcessor.ProcessPayment(payment);

			// Additional tax should be added (14% of 25 = 3.5)
			Assert.AreEqual(10.5m, invoice.TaxAmount);
		}

		[Test]
		public void ProcessPayment_Should_HandleNullPaymentReference()
		{
			var repo = new InvoiceRepository();
			var paymentProcessor = new InvoiceService(repo);
			var payment = new Payment { Reference = null };

			Assert.Throws<InvalidOperationException>(() => paymentProcessor.ProcessPayment(payment));
		}
	}
}