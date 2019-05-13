Imports KiteConnect

Namespace Entities
    Public Class ZerodhaTick
        Implements ITick
        Public ReadOnly Property InstrumentToken As String Implements ITick.InstrumentToken
            Get
                Return WrappedTick.InstrumentToken
            End Get
        End Property
        Public ReadOnly Property Tradable As Boolean Implements ITick.Tradable
            Get
                Return WrappedTick.Tradable
            End Get
        End Property
        Public ReadOnly Property Open As Decimal Implements ITick.Open
            Get
                Return WrappedTick.Open
            End Get
        End Property
        Public ReadOnly Property High As Decimal Implements ITick.High
            Get
                Return WrappedTick.High
            End Get
        End Property
        Public ReadOnly Property Low As Decimal Implements ITick.Low
            Get
                Return WrappedTick.Low
            End Get
        End Property
        Public ReadOnly Property Close As Decimal Implements ITick.Close
            Get
                Return WrappedTick.Close
            End Get
        End Property
        Public ReadOnly Property Volume As Long Implements ITick.Volume
            Get
                Return WrappedTick.Volume
            End Get
        End Property
        Public ReadOnly Property AveragePrice As Decimal Implements ITick.AveragePrice
            Get
                Return WrappedTick.AveragePrice
            End Get
        End Property
        Public ReadOnly Property LastPrice As Decimal Implements ITick.LastPrice
            Get
                Return WrappedTick.LastPrice
            End Get
        End Property
        Public ReadOnly Property Timestamp As Date? Implements ITick.Timestamp
            Get
                Return WrappedTick.Timestamp
            End Get
        End Property
        Public ReadOnly Property LastTradeTime As Date? Implements ITick.LastTradeTime
            Get
                Return WrappedTick.LastTradeTime
            End Get
        End Property

        Public Property WrappedTick As Tick
        Public ReadOnly Property Broker As APISource Implements ITick.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
    End Class
End Namespace
