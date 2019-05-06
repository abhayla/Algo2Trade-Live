Namespace Entities
    Public Class PayloadToPairConsumer
        Implements IPayloadConsumer
        Private _TypeOfConsumer As IPayloadConsumer.ConsumerType
        Public ReadOnly Property TypeOfConsumer As IPayloadConsumer.ConsumerType Implements IPayloadConsumer.TypeOfConsumer
            Get
                Return _TypeOfConsumer
            End Get
        End Property
        Public Property ConsumerPayloads As Concurrent.ConcurrentDictionary(Of Date, IPayload) Implements IPayloadConsumer.ConsumerPayloads
        Public Property OnwardLevelConsumers As List(Of IPayloadConsumer) Implements IPayloadConsumer.OnwardLevelConsumers
        Public Sub New()
            _TypeOfConsumer = IPayloadConsumer.ConsumerType.Pair
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}", Me.GetType.Name)
        End Function
    End Class
End Namespace
