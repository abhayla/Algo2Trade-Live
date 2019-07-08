Imports KiteConnect

Namespace Entities
    Public Class ZerodhaOrder
        Implements IOrder

        Public Property WrappedOrder As Order

        Public ReadOnly Property OrderIdentifier As String Implements IOrder.OrderIdentifier
            Get
                Return WrappedOrder.OrderId
            End Get
        End Property

        Public ReadOnly Property Broker As APISource Implements IOrder.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property

        Public ReadOnly Property TriggerPrice As Decimal Implements IOrder.TriggerPrice
            Get
                Return WrappedOrder.TriggerPrice
            End Get
        End Property

        Public ReadOnly Property RawTransactionType As String Implements IOrder.RawTransactionType
            Get
                Return WrappedOrder.TransactionType
            End Get
        End Property

        Public ReadOnly Property TransactionType As IOrder.TypeOfTransaction Implements IOrder.TransactionType
            Get
                Select Case RawTransactionType.ToUpper
                    Case "BUY"
                        Return IOrder.TypeOfTransaction.Buy
                    Case "SELL"
                        Return IOrder.TypeOfTransaction.Sell
                    Case Else
                        Return IOrder.TypeOfTransaction.None
                End Select
            End Get
        End Property

        Public ReadOnly Property Tradingsymbol As String Implements IOrder.Tradingsymbol
            Get
                Return WrappedOrder.Tradingsymbol
            End Get
        End Property

        Public ReadOnly Property RawStatus As String Implements IOrder.RawStatus
            Get
                Return WrappedOrder.Status
            End Get
        End Property

        Public ReadOnly Property Status As IOrder.TypeOfStatus Implements IOrder.Status
            Get
                Select Case RawStatus.ToUpper
                    Case "COMPLETE"
                        Return IOrder.TypeOfStatus.Complete
                    Case "CANCELLED"
                        Return IOrder.TypeOfStatus.Cancelled
                    Case "REJECTED"
                        Return IOrder.TypeOfStatus.Rejected
                    Case "OPEN"
                        Return IOrder.TypeOfStatus.Open
                    Case "TRIGGER PENDING"
                        Return IOrder.TypeOfStatus.TriggerPending
                    Case Else
                        Return IOrder.TypeOfStatus.None
                End Select
            End Get
        End Property

        Public ReadOnly Property StatusMessage As String Implements IOrder.StatusMessage
            Get
                Return WrappedOrder.StatusMessage
            End Get
        End Property

        Public ReadOnly Property Quantity As Integer Implements IOrder.Quantity
            Get
                Return WrappedOrder.Quantity
            End Get
        End Property

        Public ReadOnly Property Price As Decimal Implements IOrder.Price
            Get
                Return WrappedOrder.Price
            End Get
        End Property

        Public ReadOnly Property PendingQuantity As Integer Implements IOrder.PendingQuantity
            Get
                Return WrappedOrder.PendingQuantity
            End Get
        End Property

        Public ReadOnly Property InstrumentIdentifier As String Implements IOrder.InstrumentIdentifier
            Get
                Return WrappedOrder.InstrumentToken
            End Get
        End Property

        Public ReadOnly Property FilledQuantity As Integer Implements IOrder.FilledQuantity
            Get
                Return WrappedOrder.FilledQuantity
            End Get
        End Property

        Public ReadOnly Property AveragePrice As Decimal Implements IOrder.AveragePrice
            Get
                Return WrappedOrder.AveragePrice
            End Get
        End Property

        Public ReadOnly Property ParentOrderIdentifier As String Implements IOrder.ParentOrderIdentifier
            Get
                Return WrappedOrder.ParentOrderId
            End Get
        End Property

        Public ReadOnly Property Tag As String Implements IOrder.Tag
            Get
                Return WrappedOrder.Tag
            End Get
        End Property

        Public ReadOnly Property TimeStamp As Date Implements IOrder.TimeStamp
            Get
                Return WrappedOrder.OrderTimestamp
            End Get
        End Property

        Public ReadOnly Property RawOrderType As String Implements IOrder.RawOrderType
            Get
                Return WrappedOrder.OrderType
            End Get
        End Property

        Public ReadOnly Property OrderType As IOrder.TypeOfOrder Implements IOrder.OrderType
            Get
                Select Case RawOrderType.ToUpper
                    Case "MARKET"
                        Return IOrder.TypeOfOrder.Market
                    Case "LIMIT"
                        Return IOrder.TypeOfOrder.Limit
                    Case "SL"
                        Return IOrder.TypeOfOrder.SL
                    Case "SL-M"
                        Return IOrder.TypeOfOrder.SL_M
                    Case Else
                        Return IOrder.TypeOfOrder.None
                End Select
            End Get
        End Property

        Public Property LogicalOrderType As IOrder.LogicalTypeOfOrder Implements IOrder.LogicalOrderType

    End Class
End Namespace
