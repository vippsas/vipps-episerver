using EPiServer.Commerce.Order;
using Vipps.Extensions;

namespace Vipps.Helpers
{
    public static class OrderNoteHelper
    {
        public static void AddNoteAndSaveChanges(IOrderGroup orderGroup, IPayment payment, string transactionType, string noteMessage)
        {
            var noteTitle = $"{payment.PaymentMethodName} {transactionType.ToLower()}";

            orderGroup.AddNote(noteTitle, $"Payment {transactionType.ToLower()}: {noteMessage}");
        }
    }
}