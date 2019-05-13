Namespace Entities.Indicators
    Public Class EMAConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property EMAPeriod As Integer
        Public ReadOnly Property EMAField As Enums.TypeOfField
        Public ReadOnly Property SupportingSMAConsumer As SMAConsumer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal emaPeriod As Integer, ByVal emaField As Enums.TypeOfField)
            MyBase.New(associatedParentConsumer)
            Me.EMAPeriod = emaPeriod
            Me.EMAField = emaField
            Me.SupportingSMAConsumer = New SMAConsumer(associatedParentConsumer, emaPeriod, emaField)
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.EMAPeriod, Me.EMAField.ToString)
        End Function
        Class EMAPayload
            Implements IPayload
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property EMA As Field
            Public Sub New()
                Me.EMA = New Field(TypeOfField.EMA)
            End Sub
            Public Overrides Function ToString() As String
                Return String.Format("EMA:{0}", Me.EMA.Value)
            End Function
        End Class
    End Class
End Namespace