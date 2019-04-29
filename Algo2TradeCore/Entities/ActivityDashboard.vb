Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Script.Serialization
Imports System.Xml.Serialization
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Strategies

Namespace Entities
    <Serializable>
    Public Class ActivityDashboard
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private ReadOnly _defaultDate As Date = New Date(2000, 1, 1)
        Public Sub New(ByVal associatedStrategyInstrument As StrategyInstrument)
            Me.ParentStrategyInstrument = associatedStrategyInstrument
            EntryActivity = New Activity(ActivityType.Entry, Me) With {
                .PreviousActivityAttributes = New Activity(ActivityType.Entry, Me)
            }
            TargetModifyActivity = New Activity(ActivityType.TargetModify, Me) With {
                .PreviousActivityAttributes = New Activity(ActivityType.TargetModify, Me)
            }
            StoplossModifyActivity = New Activity(ActivityType.StoplossModify, Me) With {
                .PreviousActivityAttributes = New Activity(ActivityType.StoplossModify, Me)
            }
            CancelActivity = New Activity(ActivityType.Cancel, Me) With {
                .PreviousActivityAttributes = New Activity(ActivityType.Cancel, Me)
            }
            Me.SignalDirection = IOrder.TypeOfTransaction.None
            Me.SignalGeneratedTime = _defaultDate
        End Sub

        <Display(Name:="Symbol", Order:=0)>
        Public Property TradingSymbol As String

        Private _TotalExecutedOrders As Integer
        <Display(Name:="Total Executed Orders", Order:=1)>
        Public ReadOnly Property TotalExecutedOrders As Integer
            Get
                _TotalExecutedOrders = ParentStrategyInstrument.GetTotalExecutedOrders()
                Return _TotalExecutedOrders
            End Get
        End Property
        Public Function GetDirtyTotalExecutedOrders() As Integer
            Return _TotalExecutedOrders
        End Function

        Private _OverallPL As Decimal
        <Display(Name:="Overall PL", Order:=2)>
        Public ReadOnly Property OverallPL As Decimal
            Get
                _OverallPL = ParentStrategyInstrument.GetOverallPL()
                Return _OverallPL
            End Get
        End Property
        Public Function GetDirtyOverallPL() As Decimal
            Return _OverallPL
        End Function

        Private _SignalPL As Decimal
        <Display(Name:="Signal PL", Order:=3)>
        Public ReadOnly Property SignalPL As Decimal
            Get
                If ParentOrderID IsNot Nothing Then
                    _SignalPL = ParentStrategyInstrument.GetTotalPLOfAnOrder(ParentOrderID)
                End If
                Return _SignalPL
            End Get
        End Property
        Public Function GetDirtySignalPL() As Decimal
            Return _SignalPL
        End Function

        Private _ActiveSignal As Boolean
        <Display(Name:="Active Signal", Order:=4)>
        Public ReadOnly Property ActiveSignal As Boolean
            Get
                If ParentStrategyInstrument.IsActiveInstrument() Then
                    If Me.EntryActivity.RequestStatus = SignalStatusType.Activated OrElse
                       Me.EntryActivity.RequestStatus = SignalStatusType.Running Then
                        _ActiveSignal = True
                    Else
                        _ActiveSignal = False
                    End If
                Else
                    _ActiveSignal = False
                End If
                Return _ActiveSignal
            End Get
        End Property
        Public Function GetDirtyActiveSignal() As Boolean
            Return _ActiveSignal
        End Function

        <Display(Name:="Signal Generated Time", Order:=5)>
        Public Property SignalGeneratedTime As Date

        <Display(Name:="Signal Direction", Order:=6)>
        Public Property SignalDirection As IOrder.TypeOfTransaction

        <System.ComponentModel.Browsable(False)>
        Public Property EntryActivity As Activity
        <Display(Name:="Entry Request Time", Order:=7)>
        Public ReadOnly Property EntryRequestTime As Date
            Get
                Return EntryActivity.RequestTime
            End Get
        End Property
        <Display(Name:="Entry Request Status", Order:=8)>
        Public ReadOnly Property EntryRequestStatus As SignalStatusType
            Get
                Return EntryActivity.RequestStatus
            End Get
        End Property

        <System.ComponentModel.Browsable(False)>
        Public Property TargetModifyActivity As Activity
        <Display(Name:="Target Modify Request Time", Order:=9)>
        Public ReadOnly Property TargetModifyRequestTime As Date
            Get
                Return TargetModifyActivity.RequestTime
            End Get
        End Property
        <Display(Name:="Target Modify Request Status", Order:=10)>
        Public ReadOnly Property TargetModifyRequestStatus As SignalStatusType
            Get
                Return TargetModifyActivity.RequestStatus
            End Get
        End Property

        <Display(Name:="Target Modify Remarks", Order:=11)>
        Public ReadOnly Property TargetModifyRemarks As String
            Get
                Return TargetModifyActivity.RequestRemarks
            End Get
        End Property

        <System.ComponentModel.Browsable(False)>
        Public Property StoplossModifyActivity As Activity
        <Display(Name:="Stoploss Modify Request Time", Order:=12)>
        Public ReadOnly Property StoplossModifyRequestTime As Date
            Get
                Return StoplossModifyActivity.RequestTime
            End Get
        End Property
        <Display(Name:="Stoploss Modify Request Status", Order:=13)>
        Public ReadOnly Property StoplossModifyRequestStatus As SignalStatusType
            Get
                Return StoplossModifyActivity.RequestStatus
            End Get
        End Property

        <System.ComponentModel.Browsable(False)>
        Public Property CancelActivity As Activity
        <Display(Name:="Exit Request Time", Order:=14)>
        Public ReadOnly Property CancelRequestTime As Date
            Get
                Return CancelActivity.RequestTime
            End Get
        End Property
        <Display(Name:="Exit Request Status", Order:=15)>
        Public ReadOnly Property CancelRequestStatus As SignalStatusType
            Get
                Return CancelActivity.RequestStatus
            End Get
        End Property

        <Display(Name:="Exit Request Remarks", Order:=16)>
        Public ReadOnly Property CancelRequestRemarks As String
            Get
                Return CancelActivity.RequestRemarks
            End Get
        End Property

        Private _LastPrice As Decimal
        <Display(Name:="Last Price", Order:=17)>
        Public ReadOnly Property LastPrice As Decimal
            Get
                If ParentStrategyInstrument.TradableInstrument.LastTick IsNot Nothing Then
                    _LastPrice = ParentStrategyInstrument.TradableInstrument.LastTick.LastPrice
                End If
                Return _LastPrice
            End Get
        End Property
        Public Function GetDirtyLastPrice() As Decimal
            Return _LastPrice
        End Function

        Private _Timestamp As Date?
        <Display(Name:="Timestamp", Order:=18)>
        Public ReadOnly Property Timestamp As Date?
            Get
                If ParentStrategyInstrument.TradableInstrument.LastTick IsNot Nothing Then
                    _Timestamp = ParentStrategyInstrument.TradableInstrument.LastTick.Timestamp
                End If
                Return _Timestamp
            End Get
        End Property
        Public Function GetDirtyTimestamp() As Date?
            Return _Timestamp
        End Function

        Private _LastCandleTime As Date
        <Display(Name:="Last Candle Time", Order:=19)>
        Public ReadOnly Property LastCandleTime As Date
            Get
                If ParentStrategyInstrument.TradableInstrument.RawPayloads IsNot Nothing AndAlso
                    ParentStrategyInstrument.TradableInstrument.RawPayloads.Count > 0 Then
                    _LastCandleTime = ParentStrategyInstrument.TradableInstrument.RawPayloads.Keys.Max
                Else
                    _LastCandleTime = New Date
                End If
                Return _LastCandleTime
            End Get
        End Property
        Public Function GetDirtyLastCandleTime() As Date
            Return _LastCandleTime
        End Function

        <Display(Name:="Parent Order ID", Order:=20)>
        Public Property ParentOrderID As String

        <NonSerialized>
        Private _ParentStrategyInstrument As StrategyInstrument
        <System.ComponentModel.Browsable(False)>
        Public Property ParentStrategyInstrument As StrategyInstrument
            Get
                Return _ParentStrategyInstrument
            End Get
            Set(value As StrategyInstrument)
                _ParentStrategyInstrument = value
            End Set
        End Property

#Region "Activity"
        <Serializable>
        Public Class Activity

            Private ReadOnly _defaultDate As Date = New Date(2000, 1, 1)
            Public Sub New(ByVal typeOfActivity As ActivityType, ByVal parentActivityDashboard As ActivityDashboard)
                Me.TypeOfActivity = typeOfActivity
                Me.ParentActivityDashboard = parentActivityDashboard
                Me.RequestStatus = SignalStatusType.None
                Me.RequestTime = _defaultDate
                Me.ReceivedTime = _defaultDate
            End Sub
            Public ReadOnly Property TypeOfActivity As ActivityType
            Public ReadOnly Property ParentActivityDashboard As ActivityDashboard
            Public Property ActivityChanged As Boolean

            Private _RequestTime As Date
            Public Property RequestTime As Date
                Get
                    Return _RequestTime
                End Get
                Set(value As Date)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.RequestTime = _RequestTime
                    _RequestTime = value
                    Me.ActivityChanged = True
                End Set
            End Property

            Private _ReceivedTime As Date
            Public Property ReceivedTime As Date
                Get
                    Return _ReceivedTime
                End Get
                Set(value As Date)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.ReceivedTime = _ReceivedTime
                    _ReceivedTime = value
                    Me.ActivityChanged = True
                End Set
            End Property

            Private _RequestStatus As SignalStatusType
            Public Property RequestStatus As SignalStatusType
                Get
                    Return _RequestStatus
                End Get
                Set(value As SignalStatusType)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.RequestStatus = _RequestStatus
                    _RequestStatus = value
                    Me.ActivityChanged = True
                End Set
            End Property

            Private _RequestRemarks As String
            Public Property RequestRemarks As String
                Get
                    Return _RequestRemarks
                End Get
                Set(value As String)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.RequestRemarks = _RequestRemarks
                    _RequestRemarks = value
                End Set
            End Property

            <NonSerialized>
            Private _LastException As Exception
            <ScriptIgnore()>
            Public Property LastException As Exception
                Get
                    Return _LastException
                End Get
                Set(value As Exception)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.LastException = _LastException
                    _LastException = value
                End Set
            End Property

            Private _Supporting As String
            Public Property Supporting As String
                Get
                    Return _Supporting
                End Get
                Set(value As String)
                    If PreviousActivityAttributes IsNot Nothing Then PreviousActivityAttributes.Supporting = _Supporting
                    _Supporting = value
                End Set
            End Property
            Public Property PreviousActivityAttributes As Activity
        End Class
#End Region

#Region "Enum"
        Public Enum ActivityType
            Entry = 1
            TargetModify
            StoplossModify
            Cancel
            None
        End Enum
        Public Enum SignalStatusType
            Handled = 1
            Activated
            Running
            Completed
            Cancelled
            Rejected
            Discarded
            None
        End Enum
#End Region
        'Signal Status flow diagram
        'Entry Activity: Handled->Activated->Running->Complete/Cancelled/Rejected/Discarded
        'Modify/Cancel Activity: Handled->Activated->Complete/Rejected
        Public Sub NotifyPropertyChanged(ByVal p As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(p))
        End Sub
    End Class
End Namespace
