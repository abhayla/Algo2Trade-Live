Namespace Entities
    Public Interface IQuote
        ReadOnly Property InstrumentToken As String
        ReadOnly Property Open As Decimal
        ReadOnly Property High As Decimal
        ReadOnly Property Low As Decimal
        ReadOnly Property Close As Decimal
        ReadOnly Property Volume As Long
        ReadOnly Property AveragePrice As Decimal
        ReadOnly Property LastPrice As Decimal
        ReadOnly Property Timestamp As Date?

        ReadOnly Property Broker As APISource
    End Interface
End Namespace