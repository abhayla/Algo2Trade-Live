Namespace Entities
    Public Interface IInstrument
        Property InstrumentIdentifier As String
        ReadOnly Property RawExchange As String
        ReadOnly Property Expiry As Date?
        ReadOnly Property RawInstrumentType As String
        ReadOnly Property LotSize As UInteger
        ReadOnly Property Segment As String
        ReadOnly Property TickSize As Decimal
        ReadOnly Property TradingSymbol As String
        ReadOnly Property RawInstrumentName As String
        Property QuantityMultiplier As Long
        Property BrokerageCategory As String
        ReadOnly Property Broker As APISource
        Property LastTick As ITick
        Property RawPayloads As Concurrent.ConcurrentDictionary(Of Date, OHLCPayload)
        Property TickPayloads As Concurrent.ConcurrentBag(Of ITick)
        Property IsHistoricalCompleted As Boolean
        ReadOnly Property InstrumentType As TypeOfInstrument
        Property ExchangeDetails As Exchange

        <Serializable>
        Enum TypeOfInstrument
            Cash = 1
            Futures
            Options
            None
        End Enum

    End Interface
End Namespace
