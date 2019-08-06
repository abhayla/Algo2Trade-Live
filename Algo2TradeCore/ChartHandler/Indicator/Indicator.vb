Imports NLog
Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities.Indicators
Imports Algo2TradeCore.ChartHandler.ChartStyle
Imports System.Drawing

Namespace ChartHandler.Indicator
    Public Class IndicatorManeger

#Region "Events/Event handlers"
        Public Event DocumentDownloadCompleteEx(ByVal source As List(Of Object))
        Public Event DocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
        Public Event HeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
        Public Event WaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadCompleteEx(ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentDownloadCompleteEx(source)
        End Sub
        Protected Overridable Sub OnDocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, source)
        End Sub
        Protected Overridable Sub OnHeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent HeartbeatEx(msg, source)
        End Sub
        Protected Overridable Sub OnWaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent WaitingForEx(elapsedSecs, totalSecs, msg, source)
        End Sub
        Protected Overridable Sub OnDocumentDownloadComplete()
            RaiseEvent DocumentDownloadCompleteEx(New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnHeartbeat(ByVal msg As String)
            RaiseEvent HeartbeatEx(msg, New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
            RaiseEvent WaitingForEx(elapsedSecs, totalSecs, msg, New List(Of Object) From {Me})
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Property ParentController As APIStrategyController
        Private ReadOnly _parentChart As CandleStickChart
        Private ReadOnly _cts As New CancellationTokenSource
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                      ByVal assoicatedParentChart As CandleStickChart,
                      ByVal canceller As CancellationTokenSource)
            Me.ParentController = associatedParentController
            _parentChart = assoicatedParentChart
            _cts = canceller
        End Sub

#Region "Public Functions"
        Public Sub CalculateSMA(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SMAConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then
                Dim requiredDataSet As IEnumerable(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function)

                For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                         Return x
                                                                     End Function)
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
                    Dim previousNInputFieldPayloadDate As Date = GetSubPayloadStartDate(outputConsumer.ParentConsumer.ConsumerPayloads,
                                                                                        runningInputDate,
                                                                                        outputConsumer.SMAPeriod,
                                                                                        True).Item1
                    Dim smaValue As SMAConsumer.SMAPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, smaValue) Then
                        smaValue = New SMAConsumer.SMAPayload
                    End If
                    Dim requiredData As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                        outputConsumer.ParentConsumer.ConsumerPayloads.Where(Function(x)
                                                                                 Return x.Key >= previousNInputFieldPayloadDate AndAlso
                                                                                        x.Key <= runningInputDate
                                                                             End Function)

                    If requiredData IsNot Nothing AndAlso requiredData.Count > 0 Then
                        Select Case outputConsumer.SMAField
                            Case TypeOfField.Close
                                smaValue.SMA.Value = requiredData.Sum(Function(s)
                                                                          Return CType(CType(s.Value, OHLCPayload).ClosePrice.Value, Decimal)
                                                                      End Function) / requiredData.Count
                            Case TypeOfField.Spread
                                smaValue.SMA.Value = requiredData.Sum(Function(s)
                                                                          Return CType(CType(s.Value, SpreadRatioConsumer.SpreadRatioPayload).Spread.Value, Decimal)
                                                                      End Function) / requiredData.Count
                            Case TypeOfField.Ratio
                                smaValue.SMA.Value = requiredData.Sum(Function(s)
                                                                          Return CType(CType(s.Value, SpreadRatioConsumer.SpreadRatioPayload).Ratio.Value, Decimal)
                                                                      End Function) / requiredData.Count
                        End Select
                    End If
                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, smaValue, Function(key, value) smaValue)
                Next
            End If
        End Sub
        Public Sub CalculateEMA(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As EMAConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As IEnumerable(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function)

                For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                         Return x
                                                                     End Function)
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim emaValue As EMAConsumer.EMAPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, emaValue) Then
                        emaValue = New EMAConsumer.EMAPayload
                    End If

                    Dim previousEMAValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                    Dim previousEMAValue As EMAConsumer.EMAPayload = Nothing
                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        previousEMAValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                      Return x.Key < runningInputDate
                                                                                  End Function)
                        If previousEMAValues IsNot Nothing AndAlso previousEMAValues.Count > 0 Then
                            previousEMAValue = previousEMAValues.OrderBy(Function(y)
                                                                             Return y.Key
                                                                         End Function).LastOrDefault.Value
                        End If
                    End If
                    Select Case outputConsumer.EMAField
                        Case TypeOfField.Close
                            If previousEMAValues Is Nothing OrElse (previousEMAValues IsNot Nothing AndAlso previousEMAValues.Count < outputConsumer.EMAPeriod) Then
                                CalculateSMA(timeToCalculateFrom, outputConsumer.SupportingSMAConsumer)
                                emaValue.EMA.Value = CType(outputConsumer.SupportingSMAConsumer.ConsumerPayloads(runningInputDate), SMAConsumer.SMAPayload).SMA.Value
                            Else
                                emaValue.EMA.Value = (CType(outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate), OHLCPayload).ClosePrice.Value * (2 / (1 + outputConsumer.EMAPeriod))) + (previousEMAValue.EMA.Value * (1 - (2 / (1 + outputConsumer.EMAPeriod))))
                            End If
                    End Select
                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, emaValue, Function(key, value) emaValue)
                Next
            End If
        End Sub
        Public Sub CalculateATR(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As ATRConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
                outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then
                Dim requiredDataSet As IEnumerable(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function)

                For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                         Return x
                                                                     End Function)
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim atrValue As ATRConsumer.ATRPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, atrValue) Then
                        atrValue = New ATRConsumer.ATRPayload
                    End If

                    Dim previousATRValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                    Dim previousATRValue As ATRConsumer.ATRPayload = Nothing
                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        previousATRValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                      Return x.Key < runningInputDate
                                                                                  End Function)
                        If previousATRValues IsNot Nothing AndAlso previousATRValues.Count > 0 Then
                            previousATRValue = previousATRValues.OrderBy(Function(y)
                                                                             Return y.Key
                                                                         End Function).LastOrDefault.Value
                        End If
                    End If

                    Dim currentPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)
                    Dim highLow As Double = currentPayload.HighPrice.Value - currentPayload.LowPrice.Value
                    Dim highPClose As Double = 0
                    Dim lowPClose As Double = 0
                    Dim TR As Decimal = 0

                    If currentPayload.PreviousPayload IsNot Nothing Then
                        highPClose = Math.Abs(currentPayload.HighPrice.Value - currentPayload.PreviousPayload.ClosePrice.Value)
                        lowPClose = Math.Abs(currentPayload.LowPrice.Value - currentPayload.PreviousPayload.ClosePrice.Value)
                        TR = Math.Max(highLow, Math.Max(highPClose, lowPClose))
                    Else
                        TR = highLow
                    End If

                    If previousATRValues Is Nothing OrElse
                        (previousATRValues IsNot Nothing AndAlso previousATRValues.Count < outputConsumer.ATRPeriod) Then
                        If previousATRValues IsNot Nothing AndAlso previousATRValues.Count > 0 Then
                            atrValue.ATR.Value = (previousATRValues.Sum(Function(x)
                                                                            Return CType(CType(x.Value, ATRConsumer.ATRPayload).ATR.Value, Decimal)
                                                                        End Function) + TR) / (previousATRValues.Count + 1)
                        Else
                            atrValue.ATR.Value = TR
                        End If
                    Else
                        atrValue.ATR.Value = (previousATRValue.ATR.Value * (outputConsumer.ATRPeriod - 1) + TR) / outputConsumer.ATRPeriod
                    End If

                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, atrValue, Function(key, value) atrValue)
                Next
            End If
        End Sub
        Public Sub CalculateSupertrend(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SupertrendConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As List(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function).OrderBy(Function(x)
                                                                                                        Return x
                                                                                                    End Function).ToList

                CalculateATR(timeToCalculateFrom, outputConsumer.SupportingATRConsumer)

                For Each runningInputDate In requiredDataSet
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim supertrendValue As SupertrendConsumer.SupertrendPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, supertrendValue) Then
                        supertrendValue = New SupertrendConsumer.SupertrendPayload
                    End If

                    Dim previousSupertrendValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                    Dim previousSupertrendValue As SupertrendConsumer.SupertrendPayload = Nothing
                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        previousSupertrendValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                             Return x.Key < runningInputDate
                                                                                         End Function)
                        If previousSupertrendValues IsNot Nothing AndAlso previousSupertrendValues.Count > 0 Then
                            previousSupertrendValue = previousSupertrendValues.OrderBy(Function(y)
                                                                                           Return y.Key
                                                                                       End Function).LastOrDefault.Value
                        End If
                    End If

                    Dim currentPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)
                    If currentPayload.PreviousPayload IsNot Nothing AndAlso previousSupertrendValue IsNot Nothing Then
                        Dim basicUpperband As Decimal = ((currentPayload.HighPrice.Value + currentPayload.LowPrice.Value) / 2) + (outputConsumer.SupertrendMultiplier * CType(outputConsumer.SupportingATRConsumer.ConsumerPayloads(runningInputDate), ATRConsumer.ATRPayload).ATR.Value)
                        Dim basicLowerband As Decimal = ((currentPayload.HighPrice.Value + currentPayload.LowPrice.Value) / 2) - (outputConsumer.SupertrendMultiplier * CType(outputConsumer.SupportingATRConsumer.ConsumerPayloads(runningInputDate), ATRConsumer.ATRPayload).ATR.Value)
                        supertrendValue.FinalUpperBand = If(basicUpperband < previousSupertrendValue.FinalUpperBand Or currentPayload.PreviousPayload.ClosePrice.Value > previousSupertrendValue.FinalUpperBand, basicUpperband, previousSupertrendValue.FinalUpperBand)
                        supertrendValue.FinalLowerBand = If(basicLowerband > previousSupertrendValue.FinalLowerBand Or currentPayload.PreviousPayload.ClosePrice.Value < previousSupertrendValue.FinalLowerBand, basicLowerband, previousSupertrendValue.FinalLowerBand)
                        If previousSupertrendValue.FinalUpperBand = previousSupertrendValue.Supertrend.Value AndAlso
                            currentPayload.ClosePrice.Value <= supertrendValue.FinalUpperBand Then
                            supertrendValue.Supertrend.Value = supertrendValue.FinalUpperBand
                        ElseIf previousSupertrendValue.FinalUpperBand = previousSupertrendValue.Supertrend.Value AndAlso
                            currentPayload.ClosePrice.Value >= supertrendValue.FinalUpperBand Then
                            supertrendValue.Supertrend.Value = supertrendValue.FinalLowerBand
                        ElseIf previousSupertrendValue.FinalLowerBand = previousSupertrendValue.Supertrend.Value AndAlso
                            currentPayload.ClosePrice.Value >= supertrendValue.FinalLowerBand Then
                            supertrendValue.Supertrend.Value = supertrendValue.FinalLowerBand
                        ElseIf previousSupertrendValue.FinalLowerBand = previousSupertrendValue.Supertrend.Value AndAlso
                            currentPayload.ClosePrice.Value <= supertrendValue.FinalLowerBand Then
                            supertrendValue.Supertrend.Value = supertrendValue.FinalUpperBand
                        End If
                    Else
                        supertrendValue.Supertrend.Value = 0
                    End If
                    supertrendValue.SupertrendColor = If(currentPayload.ClosePrice.Value < supertrendValue.Supertrend.Value, Color.Red, Color.Green)
                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, supertrendValue, Function(key, value) supertrendValue)
                Next
            End If
        End Sub
        Public Sub CalculateBollinger(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As BollingerConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As List(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function).OrderBy(Function(x)
                                                                                                        Return x
                                                                                                    End Function).ToList

                CalculateSMA(timeToCalculateFrom, outputConsumer.SupportingSMAConsumer)

                For Each runningInputDate In requiredDataSet
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
                    Dim previousNInputFieldPayloadDate As Date = GetSubPayloadStartDate(outputConsumer.ParentConsumer.ConsumerPayloads,
                                                                                        runningInputDate,
                                                                                        outputConsumer.BollingerPeriod,
                                                                                        True).Item1

                    Dim bollingerValue As BollingerConsumer.BollingerPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, bollingerValue) Then
                        bollingerValue = New BollingerConsumer.BollingerPayload
                    End If
                    Dim requiredData As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                        outputConsumer.ParentConsumer.ConsumerPayloads.Where(Function(x)
                                                                                 Return x.Key >= previousNInputFieldPayloadDate AndAlso
                                                                                        x.Key <= runningInputDate
                                                                             End Function)


                    If requiredData IsNot Nothing AndAlso requiredData.Count > 0 Then
                        Dim highBand As Decimal = Nothing
                        Dim middleBand As Decimal = Nothing
                        Dim lowBand As Decimal = Nothing
                        Dim sd As Decimal = Nothing
                        Dim previousNInputData As Dictionary(Of Date, Decimal) = Nothing
                        Select Case outputConsumer.BollingerField
                            Case TypeOfField.Close
                                previousNInputData = requiredData.ToDictionary(Of Date, Decimal)(Function(x)
                                                                                                     Return x.Key
                                                                                                 End Function, Function(y)
                                                                                                                   Return CType(y.Value, OHLCPayload).ClosePrice.Value
                                                                                                               End Function)
                            Case TypeOfField.Spread
                                previousNInputData = requiredData.ToDictionary(Of Date, Decimal)(Function(x)
                                                                                                     Return x.Key
                                                                                                 End Function, Function(y)
                                                                                                                   Return CType(y.Value, SpreadRatioConsumer.SpreadRatioPayload).Spread.Value
                                                                                                               End Function)
                            Case TypeOfField.Ratio
                                previousNInputData = requiredData.ToDictionary(Of Date, Decimal)(Function(x)
                                                                                                     Return x.Key
                                                                                                 End Function, Function(y)
                                                                                                                   Return CType(y.Value, SpreadRatioConsumer.SpreadRatioPayload).Ratio.Value
                                                                                                               End Function)
                        End Select
                        If previousNInputData.Count > 2 Then
                            sd = CalculateStandardDeviationPA(previousNInputData)
                        Else
                            sd = 0
                        End If
                        middleBand = CType(outputConsumer.SupportingSMAConsumer.ConsumerPayloads(runningInputDate), SMAConsumer.SMAPayload).SMA.Value
                        highBand = middleBand + sd * outputConsumer.BollingerMultiplier
                        lowBand = middleBand - sd * outputConsumer.BollingerMultiplier

                        bollingerValue.HighBollinger.Value = highBand
                        bollingerValue.SMABollinger.Value = middleBand
                        bollingerValue.LowBollinger.Value = lowBand
                    End If
                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, bollingerValue, Function(key, value) bollingerValue)
                Next
            End If
        End Sub
        Public Sub CalculateSpreadRatio(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SpreadRatioConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As List(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function).OrderBy(Function(x)
                                                                                                        Return x
                                                                                                    End Function).ToList

                For Each runningInputDate In requiredDataSet
                    Dim currentPayload As PairPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)
                    If outputConsumer.ConsumerPayloads Is Nothing Then
                        If currentPayload.Instrument1Payload IsNot Nothing AndAlso currentPayload.Instrument2Payload IsNot Nothing Then
                            outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
                            If currentPayload.Instrument2Payload.ClosePrice.Value > currentPayload.Instrument1Payload.ClosePrice.Value Then
                                outputConsumer.HigherContract = currentPayload.Instrument2Payload
                                outputConsumer.LowerContract = currentPayload.Instrument1Payload
                            Else
                                outputConsumer.HigherContract = currentPayload.Instrument1Payload
                                outputConsumer.LowerContract = currentPayload.Instrument2Payload
                            End If
                        Else
                            Exit Sub
                        End If
                    End If

                    Dim spreadValue As SpreadRatioConsumer.SpreadRatioPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, spreadValue) Then
                        spreadValue = New SpreadRatioConsumer.SpreadRatioPayload
                    End If

                    Dim higher As OHLCPayload = Nothing
                    Dim lower As OHLCPayload = Nothing

                    If currentPayload.Instrument1Payload IsNot Nothing Then
                        If currentPayload.Instrument1Payload.TradingSymbol = outputConsumer.HigherContract.TradingSymbol Then
                            higher = currentPayload.Instrument1Payload
                        ElseIf currentPayload.Instrument1Payload.TradingSymbol = outputConsumer.LowerContract.TradingSymbol Then
                            lower = currentPayload.Instrument1Payload
                        End If
                    End If

                    If currentPayload.Instrument2Payload IsNot Nothing Then
                        If currentPayload.Instrument2Payload.TradingSymbol = outputConsumer.HigherContract.TradingSymbol Then
                            higher = currentPayload.Instrument2Payload
                        ElseIf currentPayload.Instrument2Payload.TradingSymbol = outputConsumer.LowerContract.TradingSymbol Then
                            lower = currentPayload.Instrument2Payload
                        End If
                    End If

                    If higher IsNot Nothing AndAlso lower IsNot Nothing Then
                        Select Case outputConsumer.SpreadRatioField
                            Case TypeOfField.Close
                                spreadValue.Spread.Value = higher.ClosePrice.Value - lower.ClosePrice.Value
                                spreadValue.Ratio.Value = higher.ClosePrice.Value / lower.ClosePrice.Value
                        End Select
                    ElseIf higher Is Nothing OrElse lower Is Nothing Then
                        Dim previousSpreadValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                        Dim previousSpreadValue As SpreadRatioConsumer.SpreadRatioPayload = Nothing
                        If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                            previousSpreadValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                             Return x.Key < runningInputDate
                                                                                         End Function)
                            If previousSpreadValues IsNot Nothing AndAlso previousSpreadValues.Count > 0 Then
                                previousSpreadValue = previousSpreadValues.OrderBy(Function(y)
                                                                                       Return y.Key
                                                                                   End Function).LastOrDefault.Value
                            End If
                        End If
                        If previousSpreadValue IsNot Nothing Then
                            spreadValue.Spread.Value = previousSpreadValue.Spread.Value
                            spreadValue.Ratio.Value = previousSpreadValue.Ratio.Value
                        Else
                            spreadValue.Spread.Value = 0
                            spreadValue.Ratio.Value = 0
                        End If
                    End If
                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, spreadValue, Function(key, value) spreadValue)
                Next
            End If
        End Sub
        Public Sub CalculatePivotHighLow(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As PivotHighLowConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As List(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function).OrderBy(Function(x)
                                                                                                        Return x
                                                                                                    End Function).ToList

                For Each runningInputDate In requiredDataSet
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim pivotHighLowValue As PivotHighLowConsumer.PivotHighLowPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, pivotHighLowValue) Then
                        pivotHighLowValue = New PivotHighLowConsumer.PivotHighLowPayload
                    End If

                    Dim runningPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)

                    Dim pivotHigh As Decimal = 0
                    Dim pivotHighSignalCandle As OHLCPayload = Nothing
                    Dim pivotLow As Decimal = 0
                    Dim pivotLowSignalCandle As OHLCPayload = Nothing
                    If runningPayload.PreviousPayload IsNot Nothing AndAlso runningPayload.PreviousPayload.PreviousPayload IsNot Nothing Then
                        If outputConsumer.Strict Then
                            If runningPayload.PreviousPayload.HighPrice.Value > runningPayload.HighPrice.Value AndAlso
                                runningPayload.PreviousPayload.HighPrice.Value > runningPayload.PreviousPayload.PreviousPayload.HighPrice.Value Then
                                pivotHigh = runningPayload.PreviousPayload.HighPrice.Value
                                pivotHighSignalCandle = runningPayload
                            Else
                                If outputConsumer.ConsumerPayloads.ContainsKey(runningPayload.PreviousPayload.SnapshotDateTime) Then
                                    pivotHigh = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotHigh.Value
                                    pivotHighSignalCandle = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotHighSignalCandle
                                End If
                            End If
                            If runningPayload.PreviousPayload.LowPrice.Value < runningPayload.LowPrice.Value AndAlso
                                runningPayload.PreviousPayload.LowPrice.Value < runningPayload.PreviousPayload.PreviousPayload.LowPrice.Value Then
                                pivotLow = runningPayload.PreviousPayload.LowPrice.Value
                                pivotLowSignalCandle = runningPayload
                            Else
                                If outputConsumer.ConsumerPayloads.ContainsKey(runningPayload.PreviousPayload.SnapshotDateTime) Then
                                    pivotLow = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotLow.Value
                                    pivotLowSignalCandle = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotLowSignalCandle
                                End If
                            End If
                        Else
                            If runningPayload.PreviousPayload.HighPrice.Value >= runningPayload.HighPrice.Value AndAlso
                                runningPayload.PreviousPayload.HighPrice.Value >= runningPayload.PreviousPayload.PreviousPayload.HighPrice.Value Then
                                pivotHigh = runningPayload.PreviousPayload.HighPrice.Value
                                pivotHighSignalCandle = runningPayload
                            Else
                                If outputConsumer.ConsumerPayloads.ContainsKey(runningPayload.PreviousPayload.SnapshotDateTime) Then
                                    pivotHigh = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotHigh.Value
                                    pivotHighSignalCandle = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotHighSignalCandle
                                End If
                            End If
                            If runningPayload.PreviousPayload.LowPrice.Value <= runningPayload.LowPrice.Value AndAlso
                                runningPayload.PreviousPayload.LowPrice.Value <= runningPayload.PreviousPayload.PreviousPayload.LowPrice.Value Then
                                pivotLow = runningPayload.PreviousPayload.LowPrice.Value
                                pivotLowSignalCandle = runningPayload
                            Else
                                If outputConsumer.ConsumerPayloads.ContainsKey(runningPayload.PreviousPayload.SnapshotDateTime) Then
                                    pivotLow = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotLow.Value
                                    pivotLowSignalCandle = CType(outputConsumer.ConsumerPayloads(runningPayload.PreviousPayload.SnapshotDateTime), PivotHighLowConsumer.PivotHighLowPayload).PivotLowSignalCandle
                                End If
                            End If
                        End If
                    End If

                    pivotHighLowValue.PivotHigh.Value = pivotHigh
                    pivotHighLowValue.PivotHighSignalCandle = pivotHighSignalCandle
                    pivotHighLowValue.PivotLow.Value = pivotLow
                    pivotHighLowValue.PivotLowSignalCandle = pivotLowSignalCandle

                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, pivotHighLowValue, Function(key, value) pivotHighLowValue)
                Next
            End If
        End Sub
        Public Sub CalculateFractal(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As FractalConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
            outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                Dim requiredDataSet As List(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function).OrderBy(Function(x)
                                                                                                        Return x
                                                                                                    End Function).ToList

                Dim highFractal As Decimal = 0
                Dim lowFractal As Decimal = 0
                For Each runningInputDate In requiredDataSet
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim previousFractalValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                    Dim previousFractalValue As FractalConsumer.FractalPayload = Nothing
                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        previousFractalValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                          Return x.Key < runningInputDate
                                                                                      End Function)
                        If previousFractalValues IsNot Nothing AndAlso previousFractalValues.Count > 0 Then
                            previousFractalValue = previousFractalValues.OrderBy(Function(y)
                                                                                     Return y.Key
                                                                                 End Function).LastOrDefault.Value
                        End If
                    End If
                    If previousFractalValue IsNot Nothing Then
                        highFractal = previousFractalValue.FractalHigh.Value
                        lowFractal = previousFractalValue.FractalLow.Value
                    End If

                    Dim fractalValue As FractalConsumer.FractalPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, fractalValue) Then
                        fractalValue = New FractalConsumer.FractalPayload
                    End If

                    Dim runningPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)

                    If runningPayload.PreviousPayload IsNot Nothing AndAlso
                        runningPayload.PreviousPayload.PreviousPayload IsNot Nothing Then
                        If runningPayload.PreviousPayload.HighPrice.Value < runningPayload.PreviousPayload.PreviousPayload.HighPrice.Value AndAlso
                            runningPayload.HighPrice.Value < runningPayload.PreviousPayload.PreviousPayload.HighPrice.Value Then
                            If IsFractalHighSatisfied(runningPayload.PreviousPayload.PreviousPayload, False) Then
                                highFractal = runningPayload.PreviousPayload.PreviousPayload.HighPrice.Value
                            End If
                        End If
                        If runningPayload.PreviousPayload.LowPrice.Value > runningPayload.PreviousPayload.PreviousPayload.LowPrice.Value AndAlso
                            runningPayload.LowPrice.Value > runningPayload.PreviousPayload.PreviousPayload.LowPrice.Value Then
                            If IsFractalLowSatisfied(runningPayload.PreviousPayload.PreviousPayload, False) Then
                                lowFractal = runningPayload.PreviousPayload.PreviousPayload.LowPrice.Value
                            End If
                        End If
                    End If

                    fractalValue.FractalHigh.Value = highFractal
                    fractalValue.FractalLow.Value = lowFractal

                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, fractalValue, Function(key, value) fractalValue)
                Next
            End If
        End Sub
        Public Sub CalculateTickSMA(ByVal outputConsumer As TickSMAConsumer)
            Dim inputPayload As Concurrent.ConcurrentBag(Of ITick) = _parentChart.GetTickPayloads()
            If outputConsumer IsNot Nothing AndAlso inputPayload IsNot Nothing AndAlso inputPayload.Count > 0 Then
                Dim lastPayload As ITick = inputPayload.OrderBy(Function(x)
                                                                    Return x.Timestamp
                                                                End Function).LastOrDefault

                If outputConsumer.OutputPayload Is Nothing Then outputConsumer.OutputPayload = New Concurrent.ConcurrentBag(Of TickSMAConsumer.TickSMAPayload)

                Dim requiredData As IEnumerable(Of ITick) = Nothing
                requiredData = inputPayload.Where(Function(y)
                                                      Return y.Timestamp <= lastPayload.Timestamp
                                                  End Function)

                Dim requiredDataSet As IEnumerable(Of ITick) = Nothing
                If requiredData IsNot Nothing AndAlso requiredData.Count > 0 Then
                    requiredDataSet = requiredData.OrderByDescending(Function(x)
                                                                         Return x.Timestamp
                                                                     End Function).Take(outputConsumer.SMAPeriod)
                End If

                If requiredDataSet IsNot Nothing AndAlso requiredDataSet.Count > 0 Then
                    Dim smaValue As TickSMAConsumer.TickSMAPayload = Nothing
                    smaValue = New TickSMAConsumer.TickSMAPayload(lastPayload.Timestamp)
                    smaValue.SupportedTick = lastPayload
                    'Dim oistr As String = Nothing
                    'Dim ltpstr As String = Nothing
                    Select Case outputConsumer.SMAField
                        Case TypeOfField.OI
                            smaValue.SMA.Value = requiredDataSet.Sum(Function(s)
                                                                         'oistr += s.Timestamp & "-" & s.OI & ","
                                                                         Return CType(s.OI, Decimal)
                                                                     End Function) / requiredDataSet.Count
                        Case TypeOfField.LastPrice
                            smaValue.SMA.Value = requiredDataSet.Sum(Function(s)
                                                                         'ltpstr += s.Timestamp & "-" & s.LastPrice & ","
                                                                         Return CType(s.LastPrice, Decimal)
                                                                     End Function) / requiredDataSet.Count
                    End Select
                    smaValue.Momentum = -1
                    Dim previousSMAValues As IEnumerable(Of TickSMAConsumer.TickSMAPayload) = Nothing
                    Dim previousSMAValue As TickSMAConsumer.TickSMAPayload = Nothing
                    If outputConsumer.OutputPayload IsNot Nothing AndAlso outputConsumer.OutputPayload.Count > 0 Then
                        previousSMAValues = outputConsumer.OutputPayload.Where(Function(x)
                                                                                   Return x.TimeStamp <= lastPayload.Timestamp
                                                                               End Function)
                        If previousSMAValues IsNot Nothing AndAlso previousSMAValues.Count > 0 Then
                            previousSMAValue = previousSMAValues.OrderBy(Function(y)
                                                                             Return y.TimeStamp
                                                                         End Function).LastOrDefault
                            If smaValue.SMA.Value > previousSMAValue.SMA.Value Then
                                smaValue.Momentum = 1
                            End If
                        End If
                    End If
                    Console.WriteLine(String.Format("SMAField:{0}, {1}", outputConsumer.SMAField.ToString, smaValue.ToString))
                    outputConsumer.OutputPayload.Add(smaValue)
                End If
            End If
        End Sub
        Public Sub CalculateVWAP(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As VWAPConsumer)
            If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
                outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then
                Dim requiredDataSet As IEnumerable(Of Date) =
                    outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                  Return x >= timeToCalculateFrom
                                                                              End Function)

                For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                         Return x
                                                                     End Function)
                    If outputConsumer.ConsumerPayloads Is Nothing Then outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)

                    Dim vwapValue As VWAPConsumer.VWAPPayload = Nothing
                    If Not outputConsumer.ConsumerPayloads.TryGetValue(runningInputDate, vwapValue) Then
                        vwapValue = New VWAPConsumer.VWAPPayload
                    End If

                    Dim previousVWAPValues As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                    Dim previousVWAPValue As KeyValuePair(Of Date, IPayload) = Nothing
                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        previousVWAPValues = outputConsumer.ConsumerPayloads.Where(Function(x)
                                                                                       Return x.Key < runningInputDate
                                                                                   End Function)
                        If previousVWAPValues IsNot Nothing AndAlso previousVWAPValues.Count > 0 Then
                            previousVWAPValue = previousVWAPValues.OrderBy(Function(y)
                                                                               Return y.Key
                                                                           End Function).LastOrDefault
                        End If
                    End If

                    Dim currentPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate)
                    Dim avgPrice As Decimal = (currentPayload.HighPrice.Value + currentPayload.LowPrice.Value + currentPayload.ClosePrice.Value) / 3
                    Dim avgPriceVolume As Decimal = avgPrice * currentPayload.Volume.Value

                    If previousVWAPValue.Key <> Date.MinValue AndAlso previousVWAPValue.Value IsNot Nothing AndAlso
                        previousVWAPValue.Key.Date = runningInputDate.Date Then
                        Dim previousPayload As OHLCPayload = outputConsumer.ParentConsumer.ConsumerPayloads(previousVWAPValue.Key)
                        Dim cumAvgPriceStarVolume As Decimal = CType(previousVWAPValue.Value, VWAPConsumer.VWAPPayload).VWAP.Value * previousPayload.DailyVolume
                        vwapValue.VWAP.Value = (cumAvgPriceStarVolume + avgPriceVolume) / currentPayload.DailyVolume
                    Else
                        vwapValue.VWAP.Value = avgPriceVolume / currentPayload.DailyVolume
                    End If

                    outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, vwapValue, Function(key, value) vwapValue)
                Next
            End If
        End Sub
#End Region

#Region "Private Function"
        Private Function GetSubPayloadStartDate(ByVal inputPayload As Concurrent.ConcurrentDictionary(Of Date, IPayload),
                                                ByVal beforeThisTime As Date,
                                                ByVal numberOfItemsToRetrive As Integer,
                                                ByVal includeTimePassedAsOneOftheItems As Boolean) As Tuple(Of Date, Integer)
            Dim ret As Tuple(Of Date, Integer) = Nothing
            If inputPayload IsNot Nothing Then
                Dim requiredData As IEnumerable(Of KeyValuePair(Of Date, IPayload)) = Nothing
                If includeTimePassedAsOneOftheItems Then
                    requiredData = inputPayload.Where(Function(y)
                                                          Return y.Key <= beforeThisTime
                                                      End Function)
                Else
                    requiredData = inputPayload.Where(Function(y)
                                                          Return y.Key < beforeThisTime
                                                      End Function)
                End If
                If requiredData IsNot Nothing AndAlso requiredData.Count > 0 Then
                    ret = New Tuple(Of Date, Integer)(requiredData.OrderByDescending(Function(x)
                                                                                         Return x.Key
                                                                                     End Function).Take(numberOfItemsToRetrive).LastOrDefault.Key, requiredData.Count)
                End If
            End If
            Return ret
        End Function
        Private Function CalculateStandardDeviationPA(ByVal inputPayload As Dictionary(Of Date, Decimal)) As Double
            Dim ret As Double = Nothing
            If inputPayload IsNot Nothing AndAlso inputPayload.Count > 0 Then
                Dim sum As Decimal = 0
                For Each runningPayload In inputPayload.Keys
                    sum = sum + inputPayload(runningPayload)
                Next
                Dim mean As Double = sum / inputPayload.Count
                Dim sumVariance As Double = 0
                For Each runningPayload In inputPayload.Keys
                    sumVariance = sumVariance + Math.Pow((inputPayload(runningPayload) - mean), 2)
                Next
                Dim sampleVariance As Double = sumVariance / (inputPayload.Count)
                Dim standardDeviation As Double = Math.Sqrt(sampleVariance)
                ret = standardDeviation
            End If
            Return ret
        End Function
        Private Function IsFractalHighSatisfied(ByVal candidateCandle As OHLCPayload, ByVal checkOnlyPrevious As Boolean) As Boolean
            Dim ret As Boolean = False
            If candidateCandle IsNot Nothing AndAlso
                candidateCandle.PreviousPayload IsNot Nothing AndAlso
                candidateCandle.PreviousPayload.PreviousPayload IsNot Nothing Then
                If checkOnlyPrevious AndAlso candidateCandle.PreviousPayload.HighPrice.Value < candidateCandle.HighPrice.Value Then
                    ret = True
                ElseIf candidateCandle.PreviousPayload.HighPrice.Value < candidateCandle.HighPrice.Value AndAlso
                        candidateCandle.PreviousPayload.PreviousPayload.HighPrice.Value < candidateCandle.HighPrice.Value Then
                    ret = True
                ElseIf candidateCandle.PreviousPayload.HighPrice.Value = candidateCandle.HighPrice.Value Then
                    ret = IsFractalHighSatisfied(candidateCandle.PreviousPayload, checkOnlyPrevious)
                ElseIf candidateCandle.PreviousPayload.HighPrice.Value > candidateCandle.HighPrice.Value Then
                    ret = False
                ElseIf candidateCandle.PreviousPayload.PreviousPayload.HighPrice.Value = candidateCandle.HighPrice.Value Then
                    ret = IsFractalHighSatisfied(candidateCandle.PreviousPayload.PreviousPayload, True)
                ElseIf candidateCandle.PreviousPayload.PreviousPayload.HighPrice.Value > candidateCandle.HighPrice.Value Then
                    ret = False
                End If
            End If
            Return ret
        End Function
        Private Function IsFractalLowSatisfied(ByVal candidateCandle As OHLCPayload, ByVal checkOnlyPrevious As Boolean) As Boolean
            Dim ret As Boolean = False
            If candidateCandle IsNot Nothing AndAlso
                candidateCandle.PreviousPayload IsNot Nothing AndAlso
                candidateCandle.PreviousPayload.PreviousPayload IsNot Nothing Then
                If checkOnlyPrevious AndAlso candidateCandle.PreviousPayload.LowPrice.Value > candidateCandle.LowPrice.Value Then
                    ret = True
                ElseIf candidateCandle.PreviousPayload.LowPrice.Value > candidateCandle.LowPrice.Value AndAlso
                        candidateCandle.PreviousPayload.PreviousPayload.LowPrice.Value > candidateCandle.LowPrice.Value Then
                    ret = True
                ElseIf candidateCandle.PreviousPayload.LowPrice.Value = candidateCandle.LowPrice.Value Then
                    ret = IsFractalLowSatisfied(candidateCandle.PreviousPayload, checkOnlyPrevious)
                ElseIf candidateCandle.PreviousPayload.LowPrice.Value < candidateCandle.LowPrice.Value Then
                    ret = False
                ElseIf candidateCandle.PreviousPayload.PreviousPayload.LowPrice.Value = candidateCandle.LowPrice.Value Then
                    ret = IsFractalLowSatisfied(candidateCandle.PreviousPayload.PreviousPayload, True)
                ElseIf candidateCandle.PreviousPayload.PreviousPayload.LowPrice.Value < candidateCandle.LowPrice.Value Then
                    ret = False
                End If
            End If
            Return ret
        End Function
#End Region

    End Class
End Namespace