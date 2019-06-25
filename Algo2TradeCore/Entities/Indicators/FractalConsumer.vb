Namespace Entities.Indicators
    Public Class FractalConsumer
        Inherits PayloadToIndicatorConsumer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer)
            MyBase.New(associatedParentConsumer)
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}", Me.ParentConsumer.ToString, Me.GetType.Name)
        End Function
        Class FractalPayload
            Implements IPayload
            Public Sub New()
                Me.FractalHigh = New Field(TypeOfField.Fractal)
                Me.FractalLow = New Field(TypeOfField.Fractal)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property FractalHigh As Field
            Public Property FractalLow As Field
            Public Overrides Function ToString() As String
                Return String.Format("Fractal High:{0}, Fractal Low:{1}", Me.FractalHigh.Value, Me.FractalLow.Value)
            End Function
        End Class
    End Class
End Namespace