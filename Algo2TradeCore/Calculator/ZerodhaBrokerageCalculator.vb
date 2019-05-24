Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities

Namespace Calculator
    Public Class ZerodhaBrokerageCalculator
        Inherits APIBrokerageCalculator

        Private _jsonDictionary As Dictionary(Of String, Object)
        Public Sub New(ByVal associatedParentController As ZerodhaStrategyController, canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, canceller)
        End Sub
        Public Overrides Function GetIntradayEquityBrokerage(buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayEquityBrokerage, parameters:{1},{2},{3}", Me.ToString, buy, sell, quantity)
            Dim ret As New ZerodhaBrokerageAttributes
            Dim m = buy
            Dim g = sell
            Dim v = quantity
            ret.Buy = buy
            ret.Sell = sell
            ret.Quantity = quantity
            ret.Multiplier = 1
            ret.CTT = 0

            Dim o = If((m * v * 0.0001) > 20, 20, Math.Round((m * v * 0.0001), 2))
            Dim i = If((g * v * 0.0001) > 20, 20, Math.Round((g * v * 0.0001), 2))
            Dim n = Math.Round((o + i), 2)
            Dim a = Math.Round(((m + g) * v), 2)
            Dim r = Convert.ToInt32(g * v * 0.00025)
            Dim s = Math.Round((0.0000325 * a), 2)
            Dim l = 0
            Dim c = Math.Round((0.18 * (n + s)), (2))

            ret.Turnover = a
            ret.Brokerage = n
            ret.STT = r
            ret.ExchangeFees = s
            ret.Clearing = l
            ret.GST = c
            ret.TotalTax = ret.Brokerage + ret.STT + ret.ExchangeFees + ret.Clearing + ret.GST + ret.SEBI
            Return ret
        End Function

        Public Overrides Function GetDeliveryEquityBrokerage(buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetDeliveryEquityBrokerage, parameters:{1},{2},{3}", Me.ToString, buy, sell, quantity)
            Dim ret As New ZerodhaBrokerageAttributes
            Dim m = buy
            Dim g = sell
            Dim v = quantity
            ret.Buy = buy
            ret.Sell = sell
            ret.Quantity = quantity
            ret.Multiplier = 1
            ret.CTT = 0

            'Dim t = 1.5
            'Dim e = 1.5

            Dim o = Math.Round(((m + g) * v), 2)
            Dim i = 0
            Dim n = Convert.ToInt32(0.001 * o)
            Dim a = Math.Round((0.0000325 * o), 2)
            Dim r = 0
            Dim s = Math.Round((0.18 * (i + a)), (2))

            ret.Turnover = o
            ret.Brokerage = i
            ret.STT = n
            ret.ExchangeFees = a
            ret.Clearing = r
            ret.GST = s
            ret.TotalTax = ret.Brokerage + ret.STT + ret.ExchangeFees + ret.Clearing + ret.GST + ret.SEBI
            Return ret
        End Function

        Public Overrides Function GetIntradayEquityFuturesBrokerage(buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayEquityFuturesBrokerage, parameters:{1},{2},{3}", Me.ToString, buy, sell, quantity)
            Dim ret As New ZerodhaBrokerageAttributes
            Dim m = buy
            Dim g = sell
            Dim v = quantity
            ret.Buy = buy
            ret.Sell = sell
            ret.Quantity = quantity
            ret.Multiplier = 1
            ret.CTT = 0

            'Dim t = 1.5
            'Dim e = 1.5

            Dim o = Math.Round(((m + g) * v), (2))
            Dim i = If((m * v * 0.0001) > 20, 20, Math.Round((m * v * 0.0001), (2)))
            Dim n = If((g * v * 0.0001) > 20, 20, Math.Round((g * v * 0.0001), (2)))
            Dim a = Math.Round((i + n), (2))
            Dim r = Convert.ToInt32(g * v * 0.0001)
            Dim s = Math.Round((0.000021 * o), (2))
            Dim l = Math.Round((0.000019 * o), (2))
            Dim c = 0
            Dim p = Math.Round((0.18 * (a + l)), (2))

            ret.Turnover = o
            ret.Brokerage = a
            ret.STT = r
            ret.ExchangeFees = l
            ret.Clearing = c
            ret.GST = p
            ret.TotalTax = ret.Brokerage + ret.STT + ret.ExchangeFees + ret.Clearing + ret.GST + ret.SEBI
            Return ret
        End Function

        Public Overrides Function GetIntradayCommodityFuturesBrokerage(instrument As IInstrument, buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayCommodityFuturesBrokerageAsync, parameters:{1},{2},{3},{4}", Me.ToString, instruemt.TradingSymbol, buy, sell, quantity)
            Dim ret As New ZerodhaBrokerageAttributes
            Dim stockName As String = instrument.TradingSymbol.Remove(instrument.TradingSymbol.Count - 8)
            Dim m = buy
            Dim g = sell
            Dim v = quantity
            ret.Buy = buy
            ret.Sell = sell
            ret.Quantity = quantity

            Dim t = instrument.QuantityMultiplier
            Dim e = instrument.BrokerageCategory
            ret.Multiplier = t
            Dim o = Math.Round(((m + g) * t * v), 2)
            Dim i = Nothing
            If m * t * v > 200000.0 Then
                i = 20
            Else
                i = If(m * t * v * 0.0001 > 20, 20, Math.Round((m * t * v * 0.0001), 2))
            End If
            Dim n = 0
            If g * t * v > 200000.0 Then
                n = 20
            Else
                n = If(g * t * v * 0.0001 > 20, 20, Math.Round((g * t * v * 0.0001), 2))
            End If
            Dim a = i + n
            Dim r = 0.00
            If e = "a" Then
                r = Math.Round((0.0001 * g * v * t), 2)
            End If
            Dim s = 0.00
            Dim l = 0.00
            Dim c = 0.00
            's = If(e = "a", Math.Round((0.000036 * o), 2), Math.Round((0.0000105 * o), 2))
            s = 0.0001 * g * v * t
            l = If(e = "a", Math.Round((0.000026 * o), 2), Math.Round((0.0000005 * o), 2))
            'c = Math.Round((0.00001 * o), 2)
            If stockName = "RBDPMOLEIN" And o >= 100000.0 Then
                Dim p = Convert.ToInt32(Math.Round((o / 100000.0), 2))
                s = p
            End If
            If stockName = "CASTORSEED" Then
                l = Math.Round((0.000005 * o), 2)
                'c = Math.Round((0.00001 * o), 2)
            ElseIf stockName = "RBDPMOLEIN" Then
                l = Math.Round((0.00001 * o), 2)
                'c = Math.Round((0.00001 * o), 2)
            ElseIf stockName = "PEPPER" Then
                l = Math.Round((0.0000005 * o), 2)
                'c = Math.Round((0.00001 * o), 2)
            End If
            Dim d = Math.Round((0.18 * (a + l)), 2)

            ret.Turnover = o
            ret.CTT = r
            ret.Brokerage = a
            ret.ExchangeFees = l
            ret.Clearing = c
            ret.GST = d
            ret.TotalTax = a + r + l + d + ret.SEBI
            Return ret
        End Function

        Public Overrides Function GetIntradayEquityOptionsBrokerage(buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayEquityOptionsBrokerage, parameters:{1},{2},{3}", Me.ToString, buy, sell, quantity)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetIntradayCurrencyFuturesBrokerage(buy As Decimal, sell As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayCurrencyFuturesBrokerage, parameters:{1},{2},{3}", Me.ToString, buy, sell, quantity)
            Dim ret As New ZerodhaBrokerageAttributes
            Dim m = buy
            Dim g = sell
            Dim v = quantity * 1000
            ret.Buy = buy
            ret.Sell = sell
            ret.Quantity = quantity * 1000
            ret.Multiplier = 1
            ret.CTT = 0

            'Dim t = 1.5
            'Dim e = 1.5

            Dim t = Math.Round(((m + g) * v), (2))
            Dim e = If((m * v * 0.0001) > 20, 20, Math.Round((m * v * 0.0001), (2)))
            Dim o = If((g * v * 0.0001) > 20, 20, Math.Round((g * v * 0.0001), (2)))
            Dim i = Math.Round((e + o), (2))
            Dim a = Math.Round((0.000009 * t), (2))
            Dim r = 0
            Dim n = a + r
            Dim s = Math.Round((0.18 * (i + n)), (2))

            ret.Turnover = t
            ret.Brokerage = i
            ret.ExchangeFees = a
            ret.Clearing = r
            ret.GST = s
            ret.TotalTax = ret.Brokerage + n + ret.GST + ret.SEBI
            Return ret
        End Function

        Public Overrides Function GetIntradayCurrencyOptionsBrokerage(strikePrice As Decimal, buyPremium As Decimal, sellPremium As Decimal, quantity As Integer) As IBrokerageAttributes
            'logger.Debug("{0}->GetIntradayCurrencyOptionsBrokerage, parameters:{1},{2},{3},{4}", Me.ToString, strikePrice, buyPremium, sellPremium, quantity)
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
