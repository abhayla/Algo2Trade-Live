Imports KiteConnect

Namespace Entities
    <Serializable>
    Public Class ZerodhaUserMargin
        Implements IUserMargin

        Public ReadOnly Property Enabled As Boolean Implements IUserMargin.Enabled
            Get
                Return WrappedUserMargin.Enabled
            End Get
        End Property

        Public ReadOnly Property NetAmount As Decimal Implements IUserMargin.NetAmount
            Get
                Return WrappedUserMargin.Net
            End Get
        End Property

        Public ReadOnly Property AvailableMargin As Decimal Implements IUserMargin.AvailableMargin
            Get
                Return WrappedUserMargin.Available.Cash
            End Get
        End Property

        Public ReadOnly Property UtilisedMargin As Decimal Implements IUserMargin.UtilisedMargin
            Get
                Return WrappedUserMargin.Utilised.M2MRealised
            End Get
        End Property

        Public ReadOnly Property Broker As APISource Implements IUserMargin.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property

        <NonSerialized>
        Private _WrappedUserMargin As UserMargin
        Public Property WrappedUserMargin As UserMargin
            Get
                Return _WrappedUserMargin
            End Get
            Set(value As UserMargin)
                _WrappedUserMargin = value
            End Set
        End Property
    End Class
End Namespace