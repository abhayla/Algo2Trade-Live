Namespace Entities.Indicators
    Public Class BollingerConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property BollingerPeriod As Integer
        Public ReadOnly Property BollingerMultiplier As Integer
        Public ReadOnly Property BollingerField As Enums.TypeOfField
        Public ReadOnly Property SupportingSMAConsumer As SMAConsumer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer,
                       ByVal bollingerPeriod As Integer,
                       ByVal bollingerMultiplier As Integer,
                       ByVal bollingerField As Enums.TypeOfField)
            MyBase.New(associatedParentConsumer)
            Me.BollingerPeriod = bollingerPeriod
            Me.BollingerMultiplier = bollingerMultiplier
            Me.BollingerField = bollingerField
            Me.SupportingSMAConsumer = New SMAConsumer(associatedParentConsumer, bollingerPeriod, bollingerField)
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3},{4})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.BollingerPeriod, Me.BollingerMultiplier, Me.BollingerField.ToString)
        End Function
        Class BollingerPayload
            Implements IPayload
            Public Sub New()
                Me.HighBollinger = New Field(TypeOfField.Bollinger)
                Me.SMABollinger = New Field(TypeOfField.Bollinger)
                Me.LowBollinger = New Field(TypeOfField.Bollinger)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property HighBollinger As Field
            Public Property SMABollinger As Field
            Public Property LowBollinger As Field
        End Class
    End Class
End Namespace