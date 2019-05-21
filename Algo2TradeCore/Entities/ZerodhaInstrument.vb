Imports KiteConnect
Imports Algo2TradeCore.ChartHandler.ChartStyle
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Strategies
Imports System.Xml.Serialization
Imports System.Web.Script.Serialization

Namespace Entities
    <Serializable>
    Public Class ZerodhaInstrument
        Implements IInstrument

        Public Sub New(ByVal associatedParentController As APIStrategyController, ByVal associatedIdentifer As String)
            InstrumentIdentifier = associatedIdentifer
        End Sub
        Public Property InstrumentIdentifier As String Implements IInstrument.InstrumentIdentifier
        Public ReadOnly Property RawExchange As String Implements IInstrument.RawExchange
            Get
                Return WrappedInstrument.Exchange
            End Get
        End Property

        Public ReadOnly Property Expiry As Date? Implements IInstrument.Expiry
            Get
                Return WrappedInstrument.Expiry
            End Get
        End Property

        Public ReadOnly Property RawInstrumentType As String Implements IInstrument.RawInstrumentType
            Get
                Return WrappedInstrument.InstrumentType
            End Get
        End Property

        Public ReadOnly Property LotSize As UInteger Implements IInstrument.LotSize
            Get
                Return WrappedInstrument.LotSize
            End Get
        End Property

        Public ReadOnly Property Segment As String Implements IInstrument.Segment
            Get
                Return WrappedInstrument.Segment
            End Get
        End Property

        Public ReadOnly Property TickSize As Decimal Implements IInstrument.TickSize
            Get
                Return WrappedInstrument.TickSize
            End Get
        End Property

        Public ReadOnly Property TradingSymbol As String Implements IInstrument.TradingSymbol
            Get
                Return WrappedInstrument.TradingSymbol
            End Get
        End Property

        Public ReadOnly Property RawInstrumentName As String Implements IInstrument.RawInstrumentName
            Get
                If Me.TradingSymbol.Contains("FUT") Then
                    Return Me.TradingSymbol.Remove(Me.TradingSymbol.Count - 8)
                Else
                    Return Me.TradingSymbol
                End If
            End Get
        End Property

        Public Property WrappedInstrument As Instrument

        Public ReadOnly Property Broker As APISource Implements IInstrument.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
        Public Overrides Function ToString() As String
            Return InstrumentIdentifier
        End Function
        Private _LastTick As ITick
        Public Property LastTick As ITick Implements IInstrument.LastTick
            Get
                Return _LastTick
            End Get
            Set(value As ITick)
                If value.InstrumentToken = Me.InstrumentIdentifier Then
                    _LastTick = value
                End If
            End Set
        End Property

        <ScriptIgnore()>
        Public Property RawPayloads As Concurrent.ConcurrentDictionary(Of Date, OHLCPayload) Implements IInstrument.RawPayloads
        <ScriptIgnore()>
        Public Property TickPayloads As Concurrent.ConcurrentBag(Of ITick) Implements IInstrument.TickPayloads

        Public Property IsHistoricalCompleted As Boolean Implements IInstrument.IsHistoricalCompleted

        Public ReadOnly Property InstrumentType As IInstrument.TypeOfInstrument Implements IInstrument.InstrumentType
            Get
                Select Case RawInstrumentType
                    Case "EQ"
                        Return IInstrument.TypeOfInstrument.Cash
                    Case "FUT"
                        Return IInstrument.TypeOfInstrument.Futures
                    Case "OPT", "CE", "PE"
                        Return IInstrument.TypeOfInstrument.Options
                    Case Else
                        Return IInstrument.TypeOfInstrument.None
                End Select
            End Get
        End Property

        Public Property QuantityMultiplier As Long Implements IInstrument.QuantityMultiplier

        Public Property BrokerageCategory As String Implements IInstrument.BrokerageCategory

        Public Property ExchangeDetails As Exchange Implements IInstrument.ExchangeDetails

    End Class
End Namespace
