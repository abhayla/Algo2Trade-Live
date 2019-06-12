Imports KiteConnect

Namespace Entities
    Public Class ZerodhaPosition
        Implements IPosition

        Public Property WrappedPosition As Position

        Public ReadOnly Property Quantity As Integer Implements IPosition.Quantity
            Get
                Return WrappedPosition.Quantity
            End Get
        End Property

        Public ReadOnly Property InstrumentIdentifier As UInteger Implements IPosition.InstrumentIdentifier
            Get
                Return WrappedPosition.InstrumentToken
            End Get
        End Property

        Public ReadOnly Property SellPrice As Decimal Implements IPosition.SellPrice
            Get
                Return WrappedPosition.SellPrice
            End Get
        End Property

        Public ReadOnly Property BuyPrice As Decimal Implements IPosition.BuyPrice
            Get
                Return WrappedPosition.BuyPrice
            End Get
        End Property

        Public ReadOnly Property AveragePrice As Decimal Implements IPosition.AveragePrice
            Get
                Return WrappedPosition.AveragePrice
            End Get
        End Property

        Public ReadOnly Property TradingSymbol As String Implements IPosition.TradingSymbol
            Get
                Return WrappedPosition.TradingSymbol
            End Get
        End Property

        Public ReadOnly Property Exchange As String Implements IPosition.Exchange
            Get
                Return WrappedPosition.Exchange
            End Get
        End Property

        Public ReadOnly Property Product As String Implements IPosition.Product
            Get
                Return WrappedPosition.Product
            End Get
        End Property

        Public ReadOnly Property Broker As APISource Implements IPosition.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
    End Class
End Namespace