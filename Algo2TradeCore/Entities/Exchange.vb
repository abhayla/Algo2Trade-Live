
Namespace Entities
    <Serializable>
    Public Class Exchange
        Public Sub New(ByVal exchangeType As Enums.TypeOfExchage)
            Me.ExchangeType = exchangeType
        End Sub
        Public ReadOnly Property ExchangeType As Enums.TypeOfExchage

        Private _ExchangeStartTime As Date
        Public Property ExchangeStartTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _ExchangeStartTime.Hour, _ExchangeStartTime.Minute, _ExchangeStartTime.Second)
            End Get
            Set(value As Date)
                _ExchangeStartTime = value
            End Set
        End Property

        Private _ExchangeEndTime As Date
        Public Property ExchangeEndTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _ExchangeEndTime.Hour, _ExchangeEndTime.Minute, _ExchangeEndTime.Second)
            End Get
            Set(value As Date)
                _ExchangeEndTime = value
            End Set
        End Property
    End Class
End Namespace