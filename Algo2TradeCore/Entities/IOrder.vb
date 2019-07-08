Namespace Entities
    Public Interface IOrder
        ReadOnly Property OrderIdentifier As String
        ReadOnly Property TriggerPrice As Decimal
        ReadOnly Property RawTransactionType As String
        ReadOnly Property TransactionType As TypeOfTransaction
        ReadOnly Property Tradingsymbol As String
        ReadOnly Property RawStatus As String
        ReadOnly Property Status As TypeOfStatus
        ReadOnly Property StatusMessage As String
        ReadOnly Property Quantity As Integer
        ReadOnly Property Price As Decimal
        ReadOnly Property PendingQuantity As Integer
        ReadOnly Property InstrumentIdentifier As String
        ReadOnly Property FilledQuantity As Integer
        ReadOnly Property AveragePrice As Decimal
        ReadOnly Property Tag As String
        ReadOnly Property ParentOrderIdentifier As String
        ReadOnly Property TimeStamp As Date
        ReadOnly Property RawOrderType As String
        ReadOnly Property OrderType As TypeOfOrder
        ReadOnly Property Broker As APISource
        Property LogicalOrderType As LogicalTypeOfOrder

        Enum TypeOfOrder
            Market = 1
            Limit
            SL_M
            SL
            None
        End Enum
        Enum TypeOfTransaction
            Buy = 1
            Sell
            None
        End Enum
        Enum TypeOfStatus
            Complete
            Cancelled
            Rejected
            Open
            TriggerPending
            None
        End Enum
        Enum LogicalTypeOfOrder
            Target = 1
            Stoploss
            Parent
            None
        End Enum
    End Interface
End Namespace
