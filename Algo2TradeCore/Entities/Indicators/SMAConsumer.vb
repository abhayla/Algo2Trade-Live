Namespace Entities.Indicators
    Public Class SMAConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property SMAPeriod As Integer
        Public ReadOnly Property SMAField As Enums.TypeOfField
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal smaPeriod As Integer, ByVal smaField As Enums.TypeOfField)
            'MyBase.New(Indicator.SMA)
            MyBase.New(associatedParentConsumer)
            Me.SMAPeriod = smaPeriod
            Me.SMAField = smaField
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.SMAPeriod, Me.SMAField.ToString)
        End Function
        Class SMAPayload
            Implements IPayload
            Public Sub New()
                Me.SMA = New Field(TypeOfField.SMA)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property SMA As Field
        End Class
    End Class
End Namespace