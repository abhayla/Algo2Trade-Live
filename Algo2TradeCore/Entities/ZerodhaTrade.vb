Imports KiteConnect

Namespace Entities
    Public Class ZerodhaTrade
        Implements ITrade
        Public Property TradeIdentifier As String Implements ITrade.TradeIdentifier
        Public Property WrappedTrade As Trade
        Public ReadOnly Property Broker As APISource Implements ITrade.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
    End Class
End Namespace
