Namespace Entities.Indicators
    Public Class PivotHighLowConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property Strict As Boolean
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal strict As Boolean)
            MyBase.New(associatedParentConsumer)
            Me.Strict = strict
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.Strict)
        End Function
        Class PivotHighLowPayload
            Implements IPayload
            Public Sub New()
                Me.PivotHigh = New Field(TypeOfField.PivotHigh)
                Me.PivotLow = New Field(TypeOfField.PivotLow)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property PivotHigh As Field
            Public Property PivotLow As Field
            Public Overrides Function ToString() As String
                Return String.Format("Pivot High:{0}, Pivot Low:{1}", Me.PivotHigh.Value, Me.PivotLow.Value)
            End Function
        End Class
    End Class
End Namespace