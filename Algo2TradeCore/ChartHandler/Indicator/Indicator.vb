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
        Protected _SMALock As Integer
        Protected _EMALock As Integer
        Protected _ATRLock As Integer
        Protected _SupertrendLock As Integer
        Protected _BollingerLock As Integer
        Protected _SpreadLock As Integer
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                      ByVal assoicatedParentChart As CandleStickChart,
                      ByVal canceller As CancellationTokenSource)
            Me.ParentController = associatedParentController
            _parentChart = assoicatedParentChart
            _cts = canceller
        End Sub

#Region "Public Functions"
        Public Async Function CalculateSMA(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SMAConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_SMALock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
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
            Finally
                Interlocked.Exchange(_SMALock, 0)
            End Try
        End Function
        Public Async Function CalculateEMA(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As EMAConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_EMALock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
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
                                    Await CalculateSMA(timeToCalculateFrom, outputConsumer.SupportingSMAConsumer).ConfigureAwait(False)
                                    emaValue.EMA.Value = CType(outputConsumer.SupportingSMAConsumer.ConsumerPayloads(runningInputDate), SMAConsumer.SMAPayload).SMA.Value
                                Else
                                    emaValue.EMA.Value = (CType(outputConsumer.ParentConsumer.ConsumerPayloads(runningInputDate), OHLCPayload).ClosePrice.Value * (2 / (1 + outputConsumer.EMAPeriod))) + (previousEMAValue.EMA.Value * (1 - (2 / (1 + outputConsumer.EMAPeriod))))
                                End If
                        End Select
                        outputConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, emaValue, Function(key, value) emaValue)
                    Next
                End If
            Finally
                Interlocked.Exchange(_EMALock, 0)
            End Try
        End Function
        Public Async Function CalculateATR(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As ATRConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_ATRLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
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
            Finally
                Interlocked.Exchange(_ATRLock, 0)
            End Try
        End Function
        Public Async Function CalculateSupertrend(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SupertrendConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_SupertrendLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
                outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                    Dim requiredDataSet As List(Of Date) =
                        outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                      Return x >= timeToCalculateFrom
                                                                                  End Function).OrderBy(Function(x)
                                                                                                            Return x
                                                                                                        End Function).ToList

                    Await CalculateATR(timeToCalculateFrom, outputConsumer.SupportingATRConsumer).ConfigureAwait(False)

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
            Finally
                Interlocked.Exchange(_SupertrendLock, 0)
            End Try
        End Function
        Public Async Function CalculateBollinger(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As BollingerConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_BollingerLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                If outputConsumer IsNot Nothing AndAlso outputConsumer.ParentConsumer IsNot Nothing AndAlso
                outputConsumer.ParentConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ParentConsumer.ConsumerPayloads.Count > 0 Then

                    Dim requiredDataSet As List(Of Date) =
                        outputConsumer.ParentConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                      Return x >= timeToCalculateFrom
                                                                                  End Function).OrderBy(Function(x)
                                                                                                            Return x
                                                                                                        End Function).ToList

                    Await CalculateSMA(timeToCalculateFrom, outputConsumer.SupportingSMAConsumer).ConfigureAwait(False)

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
            Finally
                Interlocked.Exchange(_BollingerLock, 0)
            End Try
        End Function
        Public Async Function CalculateSpreadRatio(ByVal timeToCalculateFrom As Date, ByVal outputConsumer As SpreadRatioConsumer) As Task
            Try
                While 1 = Interlocked.Exchange(_SpreadLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
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
                                Exit Function
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
            Finally
                Interlocked.Exchange(_SpreadLock, 0)
            End Try
        End Function
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
#End Region

    End Class
End Namespace