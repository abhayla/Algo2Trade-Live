Namespace Entities
    Public Class PairPayload
        Implements IPayload

        Public Property TradingSymbol As String Implements IPayload.TradingSymbol

        Public Property Instrument1Payload As OHLCPayload

        Public Property Instrument2Payload As OHLCPayload

        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}", If(Me.Instrument1Payload IsNot Nothing, Me.Instrument1Payload.ToString, "Nothing"),
                                 If(Me.Instrument2Payload IsNot Nothing, Me.Instrument2Payload.ToString, "Nothing"))
        End Function

        'Public Overrides Function Equals(obj As Object) As Boolean
        '    Dim compareWith As PairPayload = obj
        '    With Me
        '        Return Instrument1Payload.Equals(compareWith.Instrument1Payload) AndAlso
        '            Instrument2Payload.Equals(compareWith.Instrument2Payload)
        '    End With
        'End Function
        'Public Overridable Overloads Function Equals(ByVal instrument1Pyaload As OHLCPayload,
        '                                             ByVal instrument2Payload As OHLCPayload) As Boolean
        '    With Me
        '        Return .Instrument1Payload.Equals(instrument1Pyaload) AndAlso
        '            .Instrument2Payload.Equals(instrument2Payload)
        '    End With
        'End Function
    End Class
End Namespace