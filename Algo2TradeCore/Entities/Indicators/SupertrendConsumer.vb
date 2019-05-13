Imports System.Drawing

Namespace Entities.Indicators
    Public Class SupertrendConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property SupertrendPeriod As Integer
        Public ReadOnly Property SupertrendMultiplier As Integer
        Public ReadOnly Property SupportingATRConsumer As ATRConsumer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal supertrendPeriod As Integer, ByVal supertrendMultiplier As Integer)
            MyBase.New(associatedParentConsumer)
            Me.SupertrendPeriod = supertrendPeriod
            Me.SupertrendMultiplier = supertrendMultiplier
            Me.SupportingATRConsumer = New ATRConsumer(associatedParentConsumer, supertrendPeriod)
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.SupertrendPeriod, Me.SupertrendMultiplier)
        End Function
        Class SupertrendPayload
            Implements IPayload
            Public Sub New()
                Me.Supertrend = New Field(TypeOfField.Supertrend)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property FinalUpperBand As Decimal
            Public Property FinalLowerBand As Decimal
            Public Property Supertrend As Field
            Public Property SupertrendColor As Color
            Public Overrides Function ToString() As String
                Return String.Format("Supertrend:{0}, Color:{1}", Me.Supertrend.Value, Me.SupertrendColor.ToString)
            End Function
        End Class
    End Class
End Namespace