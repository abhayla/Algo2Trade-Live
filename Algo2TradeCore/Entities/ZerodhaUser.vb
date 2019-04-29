Imports KiteConnect
Namespace Entities
    <Serializable>
    Public Class ZerodhaUser
        Implements IUser

        'The below properties are input properties and hence not readyonly unlike other entities
        Public Property UserId As String Implements IUser.UserId
        Public Property Password As String Implements IUser.Password
        Public Property APISecret As String Implements IUser.APISecret
        Public Property APIKey As String Implements IUser.APIKey
        Public Property APIVersion As String Implements IUser.APIVersion
        Public Property API2FAPin As String Implements IUser.API2FAPin
        Public Property DaysStartingCapitals As Dictionary(Of Enums.TypeOfExchage, Decimal) Implements IUser.DaysStartingCapitals

        <NonSerialized>
        Private _WrappedUser As User
        Public Property WrappedUser As User
            Get
                Return _WrappedUser
            End Get
            Set(value As User)
                _WrappedUser = value
            End Set
        End Property

        Public ReadOnly Property Broker As APISource Implements IUser.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property

    End Class
End Namespace
