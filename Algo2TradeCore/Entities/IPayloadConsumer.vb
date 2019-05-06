Namespace Entities
    Public Interface IPayloadConsumer
        ReadOnly Property TypeOfConsumer As ConsumerType
        Property ConsumerPayloads As Concurrent.ConcurrentDictionary(Of Date, IPayload)
        Property OnwardLevelConsumers As List(Of IPayloadConsumer)
        Enum ConsumerType
            Chart = 1
            Indicator
            Pair
        End Enum
    End Interface
End Namespace