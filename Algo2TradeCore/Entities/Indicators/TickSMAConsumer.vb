Namespace Entities.Indicators
    Public Class TickSMAConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property SMAPeriod As Integer
        Public ReadOnly Property SMAField As Enums.TypeOfField
        Public Property OutputPayload As Concurrent.ConcurrentBag(Of TickSMAPayload)
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal smaPeriod As Integer, ByVal smaField As Enums.TypeOfField)
            'MyBase.New(Indicator.SMA)
            MyBase.New(associatedParentConsumer)
            Me.SMAPeriod = smaPeriod
            Me.SMAField = smaField
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.SMAPeriod, Me.SMAField.ToString)
        End Function
        Class TickSMAPayload
            Public Sub New(ByVal time As Date)
                Me.SMA = New Field(TypeOfField.SMA)
                Me.TimeStamp = time
            End Sub
            Public Property SMA As Field
            Public Property TimeStamp As Date
            Public Property Momentum As Integer
            Public Property SupportedTick As ITick
            Public Overrides Function ToString() As String
                Return String.Format("SMATimeStamp:{0}, SMA:{1}, Momentum:{2}",
                                     Me.TimeStamp, Me.SMA.Value, Me.Momentum)
            End Function
        End Class
    End Class
End Namespace