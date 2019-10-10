Namespace Entities
    Public Class PaperOrder
        Implements IOrder

        Public Property OrderIdentifier As String Implements IOrder.OrderIdentifier

        Public Property TriggerPrice As Decimal Implements IOrder.TriggerPrice

        Public Property RawTransactionType As String Implements IOrder.RawTransactionType

        Public Property TransactionType As IOrder.TypeOfTransaction Implements IOrder.TransactionType

        Public Property Tradingsymbol As String Implements IOrder.Tradingsymbol

        Public Property RawStatus As String Implements IOrder.RawStatus

        Public Property Status As IOrder.TypeOfStatus Implements IOrder.Status

        Public Property StatusMessage As String Implements IOrder.StatusMessage

        Public Property Quantity As Integer Implements IOrder.Quantity

        Public Property Price As Decimal Implements IOrder.Price

        Public Property PendingQuantity As Integer Implements IOrder.PendingQuantity

        Public Property InstrumentIdentifier As String Implements IOrder.InstrumentIdentifier

        Public Property FilledQuantity As Integer Implements IOrder.FilledQuantity

        Public Property AveragePrice As Decimal Implements IOrder.AveragePrice

        Public Property Tag As String Implements IOrder.Tag

        Public Property ParentOrderIdentifier As String Implements IOrder.ParentOrderIdentifier

        Public Property TimeStamp As Date Implements IOrder.TimeStamp

        Public Property RawOrderType As String Implements IOrder.RawOrderType

        Public Property OrderType As IOrder.TypeOfOrder Implements IOrder.OrderType

        Public Property SupportingFlag As Boolean Implements IOrder.SupportingFlag

        Public ReadOnly Property Broker As APISource Implements IOrder.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property

        Public Property LogicalOrderType As IOrder.LogicalTypeOfOrder Implements IOrder.LogicalOrderType
    End Class
End Namespace
