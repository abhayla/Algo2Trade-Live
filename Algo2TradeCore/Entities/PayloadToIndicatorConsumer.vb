Namespace Entities
    Public MustInherit Class PayloadToIndicatorConsumer
        Implements IPayloadConsumer
        'Public Sub New(ByVal typeOfIndicator As Enums.Indicator)
        '    _TypeOfConsumer = IPayloadConsumer.ConsumerType.Indicator
        '    Me.TypeOfIndicator = typeOfIndicator
        'End Sub
        Private _TypeOfConsumer As IPayloadConsumer.ConsumerType
        Public ReadOnly Property TypeOfConsumer As IPayloadConsumer.ConsumerType Implements IPayloadConsumer.TypeOfConsumer
            Get
                Return _TypeOfConsumer
            End Get
        End Property
        'Public ReadOnly Property TypeOfIndicator As Enums.Indicator
        Public ReadOnly Property ParentConsumer As IPayloadConsumer
        Public Property ConsumerPayloads As Concurrent.ConcurrentDictionary(Of Date, IPayload) Implements IPayloadConsumer.ConsumerPayloads
        Public Property OnwardLevelConsumers As List(Of IPayloadConsumer) Implements IPayloadConsumer.OnwardLevelConsumers
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer)
            _TypeOfConsumer = IPayloadConsumer.ConsumerType.Indicator
            Me.ParentConsumer = associatedParentConsumer
        End Sub
        Public MustOverride Overrides Function ToString() As String
    End Class
End Namespace
