Imports KiteConnect
Namespace Entities
    Public Class ZerodhaQuote
        Implements IQuote

        Public ReadOnly Property AveragePrice As Decimal Implements IQuote.AveragePrice
            Get
                Return WrappedQuote.AveragePrice
            End Get
        End Property

        Public ReadOnly Property Close As Decimal Implements IQuote.Close
            Get
                Return WrappedQuote.Close
            End Get
        End Property

        Public ReadOnly Property High As Decimal Implements IQuote.High
            Get
                Return WrappedQuote.High
            End Get
        End Property

        Public ReadOnly Property InstrumentToken As String Implements IQuote.InstrumentToken
            Get
                Return WrappedQuote.InstrumentToken
            End Get
        End Property

        Public ReadOnly Property LastPrice As Decimal Implements IQuote.LastPrice
            Get
                Return WrappedQuote.LastPrice
            End Get
        End Property

        Public ReadOnly Property Low As Decimal Implements IQuote.Low
            Get
                Return WrappedQuote.Low
            End Get
        End Property

        Public ReadOnly Property Open As Decimal Implements IQuote.Open
            Get
                Return WrappedQuote.Open
            End Get
        End Property

        Public ReadOnly Property Timestamp As Date? Implements IQuote.Timestamp
            Get
                Return WrappedQuote.Timestamp
            End Get
        End Property

        Public ReadOnly Property Volume As Long Implements IQuote.Volume
            Get
                Return WrappedQuote.Volume
            End Get
        End Property

        Public Property WrappedQuote As Quote
        Public ReadOnly Property Broker As APISource Implements IQuote.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
    End Class
End Namespace