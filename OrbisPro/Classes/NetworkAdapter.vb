Imports System.ComponentModel
Imports System.Management
Imports System.Globalization

Namespace ROOT.CIMV2.Win32

    ' Functions ShouldSerialize<PropertyName> are functions used by VS property browser to check if a particular property has to be serialized. These functions are added for all ValueType properties ( properties of type Int32, BOOL etc.. which cannot be set to null). These functions use Is<PropertyName>Null function. These functions are also used in the TypeConverter implementation for the properties to check for NULL value of property so that an empty value can be shown in Property browser in case of Drag and Drop in Visual studio.
    ' Functions Is<PropertyName>Null() are used to check if a property is NULL.
    ' Functions Reset<PropertyName> are added for Nullable Read/Write properties. These functions are used by VS designer in property browser to set a property to NULL.
    ' Every property added to the class for WMI property has attributes set to define its behavior in Visual Studio designer and also to define a TypeConverter to be used.
    ' Datetime conversion functions ToDateTime and ToDmtfDateTime are added to the class to convert DMTF datetime to System.DateTime and vice-versa.
    ' An Early Bound class generated for the WMI class.Win32_NetworkAdapter
    Public Class NetworkAdapter
        Inherits Component

        ' Private property to hold the WMI namespace in which the class resides.
        Private Shared ReadOnly CreatedWmiNamespace As String = "root\CimV2"
        ' Private property to hold the name of WMI class which created this class.
        Private Shared ReadOnly CreatedClassName As String = "Win32_NetworkAdapter"
        ' Private member variable to hold the ManagementScope which is used by the various methods.
        Private Shared statMgmtScope As ManagementScope = Nothing
        Private PrivateSystemProperties As ManagementSystemProperties
        ' Underlying lateBound WMI object.
        Private PrivateLateBoundObject As ManagementObject
        ' Member variable to store the 'automatic commit' behavior for the class.
        Private AutoCommitProp As Boolean
        ' Private variable to hold the embedded property representing the instance.
        Private ReadOnly embeddedObj As ManagementBaseObject
        ' The current WMI object used
        Private curObj As ManagementBaseObject
        ' Flag to indicate if the instance is an embedded object.
        Private isEmbedded As Boolean

        ' Below are different overloads of constructors to initialize an instance of the class with a WMI object.
        Public Sub New()
            InitializeObject(Nothing, Nothing, Nothing)
        End Sub

        Public Sub New(keyDeviceID As String)
            InitializeObject(Nothing, New ManagementPath(ConstructPath(keyDeviceID)), Nothing)
        End Sub

        Public Sub New(mgmtScope As ManagementScope, keyDeviceID As String)
            InitializeObject(mgmtScope, New ManagementPath(ConstructPath(keyDeviceID)), Nothing)
        End Sub

        Public Sub New(path As ManagementPath, getOptions As ObjectGetOptions)
            InitializeObject(Nothing, path, getOptions)
        End Sub

        Public Sub New(mgmtScope As ManagementScope, path As ManagementPath)
            InitializeObject(mgmtScope, path, Nothing)
        End Sub

        Public Sub New(path As ManagementPath)
            InitializeObject(Nothing, path, Nothing)
        End Sub

        Public Sub New(mgmtScope As ManagementScope, path As ManagementPath, getOptions As ObjectGetOptions)
            InitializeObject(mgmtScope, path, getOptions)
        End Sub

        Public Sub New(theObject As ManagementObject)
            Initialize()
            If CheckIfProperClass(theObject) = True Then
                PrivateLateBoundObject = theObject
                PrivateSystemProperties = New ManagementSystemProperties(PrivateLateBoundObject)
                curObj = PrivateLateBoundObject
            Else
                Throw New ArgumentException("Class name does not match.")
            End If
        End Sub

        Public Sub New(theObject As ManagementBaseObject)
            Initialize()
            If CheckIfProperClass(theObject) = True Then
                embeddedObj = theObject
                PrivateSystemProperties = New ManagementSystemProperties(theObject)
                curObj = embeddedObj
                isEmbedded = True
            Else
                Throw New ArgumentException("Class name does not match.")
            End If
        End Sub

        ' Property returns the namespace of the WMI class.
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Shared ReadOnly Property OriginatingNamespace As String
            Get
                Return "root\CimV2"
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property ManagementClassName As String
            Get
                Dim strRet = CreatedClassName
                If curObj IsNot Nothing Then
                    If curObj.ClassPath IsNot Nothing Then
                        strRet = CStr(curObj("__CLASS"))
                        If Equals(strRet, Nothing) OrElse Equals(strRet, String.Empty) Then
                            strRet = CreatedClassName
                        End If
                    End If
                End If
                Return strRet
            End Get
        End Property

        ' Property pointing to an embedded object to get System properties of the WMI object.
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property SystemProperties As ManagementSystemProperties
            Get
                Return PrivateSystemProperties
            End Get
        End Property

        ' Property returning the underlying lateBound object.
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property LateBoundObject As ManagementBaseObject
            Get
                Return curObj
            End Get
        End Property

        ' ManagementScope of the object.
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property Scope As ManagementScope
            Get
                If isEmbedded = False Then
                    Return PrivateLateBoundObject.Scope
                Else
                    Return Nothing
                End If
            End Get
            Set(value As ManagementScope)
                If isEmbedded = False Then
                    PrivateLateBoundObject.Scope = value
                End If
            End Set
        End Property

        ' Property to show the commit behavior for the WMI object. If true, WMI object will be automatically saved after each property modification.(ie. Put() is called after modification of a property).
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property AutoCommit As Boolean
            Get
                Return AutoCommitProp
            End Get
            Set(value As Boolean)
                AutoCommitProp = value
            End Set
        End Property

        ' The ManagementPath of the underlying WMI object.
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property Path As ManagementPath
            Get
                If isEmbedded = False Then
                    Return PrivateLateBoundObject.Path
                Else
                    Return Nothing
                End If
            End Get
            Set(value As ManagementPath)
                If isEmbedded = False Then
                    If CheckIfProperClass(Nothing, value, Nothing) <> True Then
                        Throw New ArgumentException("Class name does not match.")
                    End If
                    PrivateLateBoundObject.Path = value
                End If
            End Set
        End Property

        ' Public static scope property which is used by the various methods.
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Shared Property StaticScope As ManagementScope
            Get
                Return statMgmtScope
            End Get
            Set(value As ManagementScope)
                statMgmtScope = value
            End Set
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The AdapterType property reflects the network medium in use. This property may no" & "t be applicable to all types of network adapters listed within this class. Windo" & "ws NT only.")>
        Public ReadOnly Property AdapterType As String
            Get
                Return CStr(curObj(NameOf(AdapterType)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsAdapterTypeIdNull As Boolean
            Get
                If curObj(NameOf(AdapterTypeId)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The AdapterTypeId property reflects the network medium in use. This property gives the same information as the AdapterType property, except that the the information is returned in the form of an integer value that corresponds to the following: 
0 - Ethernet 802.3
1 - Token Ring 802.5
2 - Fiber Distributed Data Interface (FDDI)
3 - Wide Area Network (WAN)
4 - LocalTalk
5 - Ethernet using DIX header format
6 - ARCNET
7 - ARCNET (878.2)
8 - ATM
9 - Wireless
10 - Infrared Wireless
11 - Bpc
12 - CoWan
13 - 1394
This property may not be applicable to all types of network adapters listed within this class. Windows NT only.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property AdapterTypeId As AdapterTypeIdValues
            Get
                If curObj(NameOf(AdapterTypeId)) Is Nothing Then
                    Return CType(Convert.ToInt32(14), AdapterTypeIdValues)
                End If
                Return CType(Convert.ToInt32(curObj(NameOf(AdapterTypeId))), AdapterTypeIdValues)
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsAutoSenseNull As Boolean
            Get
                If curObj(NameOf(AutoSense)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("A boolean indicating whether the NetworkAdapter is capable of automatically deter" & "mining the speed or other communications characteristics of the attached network" & " media.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property AutoSense As Boolean
            Get
                If curObj(NameOf(AutoSense)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(AutoSense)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsAvailabilityNull As Boolean
            Get
                If curObj(NameOf(Availability)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The availability and status of the device.  For example, the Availability property indicates that the device is running and has full power (value=3), or is in a warning (4), test (5), degraded (10) or power save state (values 13-15 and 17). Regarding the power saving states, these are defined as follows: Value 13 (""Power Save - Unknown"") indicates that the device is known to be in a power save mode, but its exact status in this mode is unknown; 14 (""Power Save - Low Power Mode"") indicates that the device is in a power save state but still functioning, and may exhibit degraded performance; 15 (""Power Save - Standby"") describes that the device is not functioning but could be brought to full power 'quickly'; and value 17 (""Power Save - Warning"") indicates that the device is in a warning state, though also in a power save mode.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property Availability As AvailabilityValues
            Get
                If curObj(NameOf(Availability)) Is Nothing Then
                    Return CType(Convert.ToInt32(0), AvailabilityValues)
                End If
                Return CType(Convert.ToInt32(curObj(NameOf(Availability))), AvailabilityValues)
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property Caption As String
            Get
                Return CStr(curObj(NameOf(Caption)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsConfigManagerErrorCodeNull As Boolean
            Get
                If curObj(NameOf(ConfigManagerErrorCode)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("Indicates the Win32 Configuration Manager error code.  The following values may b" & "e returned: " & vbLf & "0      This device is working properly. " & vbLf & "1      This device is not " & "configured correctly. " & vbLf & "2      Windows cannot load the driver for this device. " & vbLf & "3" & "      The driver for this device might be corrupted, or your system may be runni" & "ng low on memory or other resources. " & vbLf & "4      This device is not working properly" & ". One of its drivers or your registry might be corrupted. " & vbLf & "5      The driver for" & " this device needs a resource that Windows cannot manage. " & vbLf & "6      The boot confi" & "guration for this device conflicts with other devices. " & vbLf & "7      Cannot filter. " & vbLf & "8" & "      The driver loader for the device is missing. " & vbLf & "9      This device is not wo" & "rking properly because the controlling firmware is reporting the resources for t" & "he device incorrectly. " & vbLf & "10     This device cannot start. " & vbLf & "11     This device fai" & "led. " & vbLf & "12     This device cannot find enough free resources that it can use. " & vbLf & "13 " & "    Windows cannot verify this device's resources. " & vbLf & "14     This device cannot wo" & "rk properly until you restart your computer. " & vbLf & "15     This device is not working " & "properly because there is probably a re-enumeration problem. " & vbLf & "16     Windows can" & "not identify all the resources this device uses. " & vbLf & "17     This device is asking f" & "or an unknown resource type. " & vbLf & "18     Reinstall the drivers for this device. " & vbLf & "19 " & "    Your registry might be corrupted. " & vbLf & "20     Failure using the VxD loader. " & vbLf & "21 " & "    System failure: Try changing the driver for this device. If that does not wo" & "rk, see your hardware documentation. Windows is removing this device. " & vbLf & "22     Th" & "is device is disabled. " & vbLf & "23     System failure: Try changing the driver for this " & "device. If that doesn't work, see your hardware documentation. " & vbLf & "24     This devi" & "ce is not present, is not working properly, or does not have all its drivers ins" & "talled. " & vbLf & "25     Windows is still setting up this device. " & vbLf & "26     Windows is stil" & "l setting up this device. " & vbLf & "27     This device does not have valid log configurat" & "ion. " & vbLf & "28     The drivers for this device are not installed. " & vbLf & "29     This device " & "is disabled because the firmware of the device did not give it the required reso" & "urces. " & vbLf & "30     This device is using an Interrupt Request (IRQ) resource that ano" & "ther device is using. " & vbLf & "31     This device is not working properly because Window" & "s cannot load the drivers required for this device.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property ConfigManagerErrorCode As ConfigManagerErrorCodeValues
            Get
                If curObj(NameOf(ConfigManagerErrorCode)) Is Nothing Then
                    Return CType(Convert.ToInt32(32), ConfigManagerErrorCodeValues)
                End If
                Return CType(Convert.ToInt32(curObj(NameOf(ConfigManagerErrorCode))), ConfigManagerErrorCodeValues)
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsConfigManagerUserConfigNull As Boolean
            Get
                If curObj(NameOf(ConfigManagerUserConfig)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("Indicates whether the device is using a user-defined configuration.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property ConfigManagerUserConfig As Boolean
            Get
                If curObj(NameOf(ConfigManagerUserConfig)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(ConfigManagerUserConfig)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("CreationClassName indicates the name of the class or the subclass used in the cre" & "ation of an instance. When used with the other key properties of this class, thi" & "s property allows all instances of this class and its subclasses to be uniquely " & "identified.")>
        Public ReadOnly Property CreationClassName As String
            Get
                Return CStr(curObj(NameOf(CreationClassName)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property Description As String
            Get
                Return CStr(curObj(NameOf(Description)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The DeviceID property contains a string uniquely identifying the network adapter " & "from other devices on the system.")>
        Public ReadOnly Property DeviceID As String
            Get
                Return CStr(curObj(NameOf(DeviceID)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsErrorClearedNull As Boolean
            Get
                If curObj(NameOf(ErrorCleared)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("ErrorCleared is a boolean property indicating that the error reported in LastErro" & "rCode property is now cleared.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property ErrorCleared As Boolean
            Get
                If curObj(NameOf(ErrorCleared)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(ErrorCleared)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("ErrorDescription is a free-form string supplying more information about the error" & " recorded in LastErrorCode property, and information on any corrective actions t" & "hat may be taken.")>
        Public ReadOnly Property ErrorDescription As String
            Get
                Return CStr(curObj(NameOf(ErrorDescription)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The GUID property specifies the Globally-unique identifier for the connection.")>
        Public ReadOnly Property GUID As String
            Get
                Return CStr(curObj(NameOf(GUID)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsIndexNull As Boolean
            Get
                If curObj(NameOf(Index)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The Index property indicates the network adapter's  index number, which is stored" & " in the system registry. " & vbLf & "Example: 0.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property Index As UInteger
            Get
                If curObj(NameOf(Index)) Is Nothing Then
                    Return Convert.ToUInt32(0)
                End If
                Return CUInt(curObj(NameOf(Index)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsInstallDateNull As Boolean
            Get
                If curObj(NameOf(InstallDate)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property InstallDate As Date
            Get
                If curObj(NameOf(InstallDate)) IsNot Nothing Then
                    Return ToDateTime(CStr(curObj(NameOf(InstallDate))))
                Else
                    Return Date.MinValue
                End If
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsInstalledNull As Boolean
            Get
                If curObj(NameOf(Installed)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The Installed property determines whether the network adapter is installed in the system.
Values: TRUE or FALSE. A value of TRUE indicates the network adapter is installed.  
The Installed property has been deprecated.  There is no replacementvalue and this property is now considered obsolete.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property Installed As Boolean
            Get
                If curObj(NameOf(Installed)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(Installed)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsInterfaceIndexNull As Boolean
            Get
                If curObj(NameOf(InterfaceIndex)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The InterfaceIndex property contains the index value that uniquely identifies the" & " local interface.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property InterfaceIndex As UInteger
            Get
                If curObj(NameOf(InterfaceIndex)) Is Nothing Then
                    Return Convert.ToUInt32(0)
                End If
                Return CUInt(curObj(NameOf(InterfaceIndex)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsLastErrorCodeNull As Boolean
            Get
                If curObj(NameOf(LastErrorCode)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("LastErrorCode captures the last error code reported by the logical device.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property LastErrorCode As UInteger
            Get
                If curObj(NameOf(LastErrorCode)) Is Nothing Then
                    Return Convert.ToUInt32(0)
                End If
                Return CUInt(curObj(NameOf(LastErrorCode)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The MACAddress property indicates the media access control address for this network adapter. A MAC address is a unique 48-bit number assigned to the network adapter by the manufacturer. It uniquely identifies this network adapter and is used for mapping TCP/IP network communications.")>
        Public ReadOnly Property MACAddress As String
            Get
                Return CStr(curObj(NameOf(MACAddress)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The Manufacturer property indicates the name of the network adapter's manufacture" & "r." & vbLf & "Example: 3COM.")>
        Public ReadOnly Property Manufacturer As String
            Get
                Return CStr(curObj(NameOf(Manufacturer)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsMaxNumberControlledNull As Boolean
            Get
                If curObj(NameOf(MaxNumberControlled)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The MaxNumberControlled property indicates the maximum number of directly address" & "able ports supported by this network adapter. A value of zero should be used if " & "the number is unknown.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property MaxNumberControlled As UInteger
            Get
                If curObj(NameOf(MaxNumberControlled)) Is Nothing Then
                    Return Convert.ToUInt32(0)
                End If
                Return CUInt(curObj(NameOf(MaxNumberControlled)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsMaxSpeedNull As Boolean
            Get
                If curObj(NameOf(MaxSpeed)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The maximum speed, in bits per second, for the network adapter.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property MaxSpeed As ULong
            Get
                If curObj(NameOf(MaxSpeed)) Is Nothing Then
                    Return Convert.ToUInt64(0)
                End If
                Return CULng(curObj(NameOf(MaxSpeed)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property Name As String
            Get
                Return CStr(curObj(NameOf(Name)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The NetConnectionID property specifies the name of the network connection as it a" & "ppears in the 'Network Connections' folder.")>
        Public Property NetConnectionID As String
            Get
                Return CStr(curObj(NameOf(NetConnectionID)))
            End Get
            Set(value As String)
                curObj(NameOf(NetConnectionID)) = value
                If isEmbedded = False AndAlso AutoCommitProp = True Then
                    PrivateLateBoundObject.Put()
                End If
            End Set
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsNetConnectionStatusNull As Boolean
            Get
                If curObj(NameOf(NetConnectionStatus)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("NetConnectionStatus is a string indicating the state of the network adapter's connection to the network. The value of the property is to be interpreted as follows:
0 - Disconnected
1 - Connecting
2 - Connected
3 - Disconnecting
4 - Hardware not present
5 - Hardware disabled
6 - Hardware malfunction
7 - Media disconnected
8 - Authenticating
9 - Authentication succeeded
10 - Authentication failed
11 - Invalid Address
12 - Credentials Required
.. - Other - For integer values other than those listed above, refer to Win32 error code documentation.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property NetConnectionStatus As UShort
            Get
                If curObj(NameOf(NetConnectionStatus)) Is Nothing Then
                    Return Convert.ToUInt16(0)
                End If
                Return CUShort(curObj(NameOf(NetConnectionStatus)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsNetEnabledNull As Boolean
            Get
                If curObj(NameOf(NetEnabled)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The NetEnabled property specifies whether the network connection is enabled or no" & "t.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property NetEnabled As Boolean
            Get
                If curObj(NameOf(NetEnabled)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(NetEnabled)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("An array of strings indicating the network addresses for an adapter.")>
        Public ReadOnly Property NetworkAddresses As String()
            Get
                Return CType(curObj(NameOf(NetworkAddresses)), String())
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("PermanentAddress defines the network address hard coded into an adapter.  This 'hard coded' address may be changed via firmware upgrade or software configuration. If so, this field should be updated when the change is made.  PermanentAddress should be left blank if no 'hard coded' address exists for the network adapter.")>
        Public ReadOnly Property PermanentAddress As String
            Get
                Return CStr(curObj(NameOf(PermanentAddress)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsPhysicalAdapterNull As Boolean
            Get
                If curObj(NameOf(PhysicalAdapter)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The PhysicalAdapter property specifies whether the adapter is physical adapter or" & " logical adapter.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property PhysicalAdapter As Boolean
            Get
                If curObj(NameOf(PhysicalAdapter)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(PhysicalAdapter)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("Indicates the Win32 Plug and Play device ID of the logical device.  Example: *PNP" & "030b")>
        Public ReadOnly Property PNPDeviceID As String
            Get
                Return CStr(curObj(NameOf(PNPDeviceID)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("Indicates the specific power-related capabilities of the logical device. The array values, 0=""Unknown"", 1=""Not Supported"" and 2=""Disabled"" are self-explanatory. The value, 3=""Enabled"" indicates that the power management features are currently enabled but the exact feature set is unknown or the information is unavailable. ""Power Saving Modes Entered Automatically"" (4) describes that a device can change its power state based on usage or other criteria. ""Power State Settable"" (5) indicates that the SetPowerState method is supported. ""Power Cycling Supported"" (6) indicates that the SetPowerState method can be invoked with the PowerState input variable set to 5 (""Power Cycle""). ""Timed Power On Supported"" (7) indicates that the SetPowerState method can be invoked with the PowerState input variable set to 5 (""Power Cycle"") and the Time parameter set to a specific date and time, or interval, for power-on.")>
        Public ReadOnly Property PowerManagementCapabilities As PowerManagementCapabilitiesValues()

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsPowerManagementSupportedNull As Boolean
            Get
                If curObj(NameOf(PowerManagementSupported)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("Boolean indicating that the Device can be power managed - ie, put into a power save state. This boolean does not indicate that power management features are currently enabled, or if enabled, what features are supported. Refer to the PowerManagementCapabilities array for this information. If this boolean is false, the integer value 1, for the string, ""Not Supported"", should be the only entry in the PowerManagementCapabilities array.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property PowerManagementSupported As Boolean
            Get
                If curObj(NameOf(PowerManagementSupported)) Is Nothing Then
                    Return Convert.ToBoolean(0)
                End If
                Return CBool(curObj(NameOf(PowerManagementSupported)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The ProductName property indicates the product name of the network adapter." & vbLf & "Examp" & "le: Fast EtherLink XL")>
        Public ReadOnly Property ProductName As String
            Get
                Return CStr(curObj(NameOf(ProductName)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The ServiceName property indicates the service name of the network adapter. This " & "name is usually shorter that the full product name. " & vbLf & "Example: Elnkii.")>
        Public ReadOnly Property ServiceName As String
            Get
                Return CStr(curObj(NameOf(ServiceName)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsSpeedNull As Boolean
            Get
                If curObj(NameOf(Speed)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("An estimate of the current bandwidth in bits per second. For endpoints which vary" & " in bandwidth or for those where no accurate estimation can be made, this proper" & "ty should contain the nominal bandwidth.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property Speed As ULong
            Get
                If curObj(NameOf(Speed)) Is Nothing Then
                    Return Convert.ToUInt64(0)
                End If
                Return CULng(curObj(NameOf(Speed)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property Status As String
            Get
                Return CStr(curObj(NameOf(Status)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsStatusInfoNull As Boolean
            Get
                If curObj(NameOf(StatusInfo)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("StatusInfo is a string indicating whether the logical device is in an enabled (va" & "lue = 3), disabled (value = 4) or some other (1) or unknown (2) state. If this p" & "roperty does not apply to the logical device, the value, 5 (""Not Applicable""), s" & "hould be used.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property StatusInfo As StatusInfoValues
            Get
                If curObj(NameOf(StatusInfo)) Is Nothing Then
                    Return CType(Convert.ToInt32(0), StatusInfoValues)
                End If
                Return CType(Convert.ToInt32(curObj(NameOf(StatusInfo))), StatusInfoValues)
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The scoping System's CreationClassName.")>
        Public ReadOnly Property SystemCreationClassName As String
            Get
                Return CStr(curObj(NameOf(SystemCreationClassName)))
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The scoping System's Name.")>
        Public ReadOnly Property SystemName As String
            Get
                Return CStr(curObj(NameOf(SystemName)))
            End Get
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public ReadOnly Property IsTimeOfLastResetNull As Boolean
            Get
                If curObj(NameOf(TimeOfLastReset)) Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Description("The TimeOfLastReset property indicates when the network adapter was last reset.")>
        <TypeConverter(GetType(WMIValueTypeConverter))>
        Public ReadOnly Property TimeOfLastReset As Date
            Get
                If curObj(NameOf(TimeOfLastReset)) IsNot Nothing Then
                    Return ToDateTime(CStr(curObj(NameOf(TimeOfLastReset))))
                Else
                    Return Date.MinValue
                End If
            End Get
        End Property

        Private Function CheckIfProperClass(mgmtScope As ManagementScope, path As ManagementPath, OptionsParam As ObjectGetOptions) As Boolean
            If path IsNot Nothing AndAlso String.Compare(path.ClassName, ManagementClassName, True, CultureInfo.InvariantCulture) = 0 Then
                Return True
            Else
                Return CheckIfProperClass(New ManagementObject(mgmtScope, path, OptionsParam))
            End If
        End Function

        Private Function CheckIfProperClass(theObj As ManagementBaseObject) As Boolean
            If theObj IsNot Nothing AndAlso String.Compare(CStr(theObj("__CLASS")), ManagementClassName, True, CultureInfo.InvariantCulture) = 0 Then
                Return True
            Else
                Dim parentClasses = CType(theObj("__DERIVATION"), Array)
                If parentClasses IsNot Nothing Then
                    Dim count As Integer = 0

                    While count < parentClasses.Length
                        If String.Compare(CStr(parentClasses.GetValue(count)), ManagementClassName, True, CultureInfo.InvariantCulture) = 0 Then
                            Return True
                        End If

                        count += 1
                    End While
                End If
            End If
            Return False
        End Function

        Private Function ShouldSerializeAdapterTypeId() As Boolean
            If IsAdapterTypeIdNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeAutoSense() As Boolean
            If IsAutoSenseNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeAvailability() As Boolean
            If IsAvailabilityNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeConfigManagerErrorCode() As Boolean
            If IsConfigManagerErrorCodeNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeConfigManagerUserConfig() As Boolean
            If IsConfigManagerUserConfigNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeErrorCleared() As Boolean
            If IsErrorClearedNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeIndex() As Boolean
            If IsIndexNull = False Then
                Return True
            End If
            Return False
        End Function

        ' Converts a given datetime in DMTF format to System.DateTime object.
        Private Shared Function ToDateTime(dmtfDate As String) As Date
            Dim initializer = Date.MinValue
            Dim year = initializer.Year
            Dim month = initializer.Month
            Dim day = initializer.Day
            Dim hour = initializer.Hour
            Dim minute = initializer.Minute
            Dim second = initializer.Second
            Dim ticks As Long = 0
            Dim dmtf = dmtfDate

            If Equals(dmtf, Nothing) Then
                Throw New ArgumentOutOfRangeException(Nothing, NameOf(dmtf))
            End If
            If dmtf.Length = 0 Then
                Throw New ArgumentOutOfRangeException(Nothing, NameOf(dmtf))
            End If
            If dmtf.Length <> 25 Then
                Throw New ArgumentOutOfRangeException(Nothing, NameOf(dmtf))
            End If
            Dim tempString As String
            Try
                tempString = dmtf.Substring(0, 4)
                If Not Equals("****", tempString) Then
                    year = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(4, 2)
                If Not Equals("**", tempString) Then
                    month = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(6, 2)
                If Not Equals("**", tempString) Then
                    day = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(8, 2)
                If Not Equals("**", tempString) Then
                    hour = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(10, 2)
                If Not Equals("**", tempString) Then
                    minute = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(12, 2)
                If Not Equals("**", tempString) Then
                    second = Integer.Parse(tempString)
                End If
                tempString = dmtf.Substring(15, 6)
                If Not Equals("******", tempString) Then
                    ticks = Long.Parse(tempString) * CLng(TimeSpan.TicksPerMillisecond / 1000)
                End If
                If year < 0 OrElse month < 0 OrElse day < 0 OrElse hour < 0 OrElse minute < 0 OrElse minute < 0 OrElse second < 0 OrElse ticks < 0 Then
                    Throw New ArgumentOutOfRangeException(Nothing, "")
                End If
            Catch e As Exception
                Throw New ArgumentOutOfRangeException(Nothing, e.Message)
            End Try
            Dim datetime As New DateTime(year, month, day, hour, minute, second, 0)
            datetime = datetime.AddTicks(ticks)

            Dim tickOffset = TimeZoneInfo.Local.GetUtcOffset(datetime)
            Dim OffsetMins As Long = CLng(tickOffset.Ticks / TimeSpan.TicksPerMinute)
            tempString = dmtf.Substring(22, 3)
            If Not Equals(tempString, "******") Then
                tempString = dmtf.Substring(21, 4)
                Dim UTCOffset As Integer
                Try
                    UTCOffset = Integer.Parse(tempString)
                Catch e As Exception
                    Throw New ArgumentOutOfRangeException(Nothing, e.Message)
                End Try
                Dim OffsetToBeAdjusted As Integer = CInt(OffsetMins - UTCOffset)
                datetime = datetime.AddMinutes(OffsetToBeAdjusted)
            End If
            Return datetime
        End Function

        ' Converts a given System.DateTime object to DMTF datetime format.
        Private Shared Function ToDmtfDateTime([date] As Date) As String
            Dim tickOffset = TimeZoneInfo.Local.GetUtcOffset([date])
            Dim OffsetMins As Long = CLng(tickOffset.Ticks / TimeSpan.TicksPerMinute)
            Dim utcString As String
            If Math.Abs(OffsetMins) > 999 Then
                [date] = [date].ToUniversalTime()
                utcString = "+000"
            Else
                If tickOffset.Ticks >= 0 Then
                    utcString = String.Concat("+", CLng(tickOffset.Ticks / TimeSpan.TicksPerMinute).ToString().PadLeft(3, "0"c))
                Else
                    Dim strTemp As String = OffsetMins.ToString()
                    utcString = String.Concat("-", strTemp.Substring(1, strTemp.Length - 1).PadLeft(3, "0"c))
                End If
            End If
            Dim dmtfDateTime As String = [date].Year.ToString().PadLeft(4, "0"c)
            dmtfDateTime = String.Concat(dmtfDateTime, [date].Month.ToString().PadLeft(2, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, [date].Day.ToString().PadLeft(2, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, [date].Hour.ToString().PadLeft(2, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, [date].Minute.ToString().PadLeft(2, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, [date].Second.ToString().PadLeft(2, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, ".")
            Dim dtTemp As New DateTime([date].Year, [date].Month, [date].Day, [date].Hour, [date].Minute, [date].Second, 0)
            Dim microsec As Long = CLng(([date].Ticks - dtTemp.Ticks) * 1000 / TimeSpan.TicksPerMillisecond)

            Dim strMicrosec As String = microsec.ToString()
            If strMicrosec.Length > 6 Then
                strMicrosec = strMicrosec.Substring(0, 6)
            End If
            dmtfDateTime = String.Concat(dmtfDateTime, strMicrosec.PadLeft(6, "0"c))
            dmtfDateTime = String.Concat(dmtfDateTime, utcString)
            Return dmtfDateTime
        End Function

        Private Function ShouldSerializeInstallDate() As Boolean
            If IsInstallDateNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeInstalled() As Boolean
            If IsInstalledNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeInterfaceIndex() As Boolean
            If IsInterfaceIndexNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeLastErrorCode() As Boolean
            If IsLastErrorCodeNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeMaxNumberControlled() As Boolean
            If IsMaxNumberControlledNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeMaxSpeed() As Boolean
            If IsMaxSpeedNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Sub ResetNetConnectionID()
            curObj(NameOf(NetConnectionID)) = Nothing
            If isEmbedded = False AndAlso AutoCommitProp = True Then
                PrivateLateBoundObject.Put()
            End If
        End Sub

        Private Function ShouldSerializeNetConnectionStatus() As Boolean
            If IsNetConnectionStatusNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeNetEnabled() As Boolean
            If IsNetEnabledNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializePhysicalAdapter() As Boolean
            If IsPhysicalAdapterNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializePowerManagementSupported() As Boolean
            If IsPowerManagementSupportedNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeSpeed() As Boolean
            If IsSpeedNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeStatusInfo() As Boolean
            If IsStatusInfoNull = False Then
                Return True
            End If
            Return False
        End Function

        Private Function ShouldSerializeTimeOfLastReset() As Boolean
            If IsTimeOfLastResetNull = False Then
                Return True
            End If
            Return False
        End Function

        <Browsable(True)>
        Public Sub CommitObject()
            If isEmbedded = False Then
                PrivateLateBoundObject.Put()
            End If
        End Sub

        <Browsable(True)>
        Public Sub CommitObject(putOptions As PutOptions)
            If isEmbedded = False Then
                PrivateLateBoundObject.Put(putOptions)
            End If
        End Sub

        Private Sub Initialize()
            AutoCommitProp = True
            isEmbedded = False
        End Sub

        Private Shared Function ConstructPath(keyDeviceID As String) As String
            Dim strPath = "root\CimV2:Win32_NetworkAdapter"
            strPath = String.Concat(strPath, String.Concat(".DeviceID=", String.Concat("""", String.Concat(keyDeviceID, """"))))
            Return strPath
        End Function

        Private Sub InitializeObject(mgmtScope As ManagementScope, path As ManagementPath, getOptions As ObjectGetOptions)
            Initialize()
            If path IsNot Nothing Then
                If CheckIfProperClass(mgmtScope, path, getOptions) <> True Then
                    Throw New ArgumentException("Class name does not match.")
                End If
            End If
            PrivateLateBoundObject = New ManagementObject(mgmtScope, path, getOptions)
            PrivateSystemProperties = New ManagementSystemProperties(PrivateLateBoundObject)
            curObj = PrivateLateBoundObject
        End Sub

        ' Different overloads of GetInstances() help in enumerating instances of the WMI class.
        Public Shared Function GetInstances() As NetworkAdapterCollection
            Return GetInstances(Nothing, Nothing, Nothing)
        End Function

        Public Shared Function GetInstances(condition As String) As NetworkAdapterCollection
            Return GetInstances(Nothing, condition, Nothing)
        End Function

        Public Shared Function GetInstances(selectedProperties As String()) As NetworkAdapterCollection
            Return GetInstances(Nothing, Nothing, selectedProperties)
        End Function

        Public Shared Function GetInstances(condition As String, selectedProperties As String()) As NetworkAdapterCollection
            Return GetInstances(Nothing, condition, selectedProperties)
        End Function

        Public Shared Function GetInstances(mgmtScope As ManagementScope, enumOptions As EnumerationOptions) As NetworkAdapterCollection
            If mgmtScope Is Nothing Then
                If statMgmtScope Is Nothing Then
                    mgmtScope = New ManagementScope()
                    mgmtScope.Path.NamespacePath = "root\CimV2"
                Else
                    mgmtScope = statMgmtScope
                End If
            End If

            Dim pathObj As New ManagementPath With {
                .ClassName = "Win32_NetworkAdapter",
                .NamespacePath = "root\CimV2"
            }
            Dim clsObject As New ManagementClass(mgmtScope, pathObj, Nothing)
            If enumOptions Is Nothing Then
                enumOptions = New EnumerationOptions With {
                    .EnsureLocatable = True
                }
            End If
            Return New NetworkAdapterCollection(clsObject.GetInstances(enumOptions))
        End Function

        Public Shared Function GetInstances(mgmtScope As ManagementScope, condition As String) As NetworkAdapterCollection
            Return GetInstances(mgmtScope, condition, Nothing)
        End Function

        Public Shared Function GetInstances(mgmtScope As ManagementScope, selectedProperties As String()) As NetworkAdapterCollection
            Return GetInstances(mgmtScope, Nothing, selectedProperties)
        End Function

        Public Shared Function GetInstances(mgmtScope As ManagementScope, condition As String, selectedProperties As String()) As NetworkAdapterCollection
            If mgmtScope Is Nothing Then
                If statMgmtScope Is Nothing Then
                    mgmtScope = New ManagementScope()
                    mgmtScope.Path.NamespacePath = "root\CimV2"
                Else
                    mgmtScope = statMgmtScope
                End If
            End If
            Dim ObjectSearcher As New ManagementObjectSearcher(mgmtScope, New SelectQuery("Win32_NetworkAdapter", condition, selectedProperties))
            Dim enumOptions As New EnumerationOptions With {
                .EnsureLocatable = True
            }
            ObjectSearcher.Options = enumOptions
            Return New NetworkAdapterCollection(ObjectSearcher.[Get]())
        End Function

        <Browsable(True)>
        Public Shared Function CreateInstance() As NetworkAdapter
            Dim mgmtScope As ManagementScope
            If statMgmtScope Is Nothing Then
                mgmtScope = New ManagementScope()
                mgmtScope.Path.NamespacePath = CreatedWmiNamespace
            Else
                mgmtScope = statMgmtScope
            End If
            Dim mgmtPath As New ManagementPath(CreatedClassName)
            Dim tmpMgmtClass As New ManagementClass(mgmtScope, mgmtPath, Nothing)
            Return New NetworkAdapter(tmpMgmtClass.CreateInstance())
        End Function

        <Browsable(True)>
        Public Sub Delete()
            PrivateLateBoundObject.Delete()
        End Sub

        Public Function Disable() As UInteger
            If isEmbedded = False Then
                Dim inParams As ManagementBaseObject = Nothing
                Dim outParams As ManagementBaseObject = PrivateLateBoundObject.InvokeMethod("Disable", inParams, Nothing)
                Return Convert.ToUInt32(outParams.Properties("ReturnValue").Value)
            Else
                Return Convert.ToUInt32(0)
            End If
        End Function

        Public Function Enable() As UInteger
            If isEmbedded = False Then
                Dim inParams As ManagementBaseObject = Nothing
                Dim outParams As ManagementBaseObject = PrivateLateBoundObject.InvokeMethod("Enable", inParams, Nothing)
                Return Convert.ToUInt32(outParams.Properties("ReturnValue").Value)
            Else
                Return Convert.ToUInt32(0)
            End If
        End Function

        Public Function Reset() As UInteger
            If isEmbedded = False Then
                Dim inParams As ManagementBaseObject = Nothing
                Dim outParams As ManagementBaseObject = PrivateLateBoundObject.InvokeMethod("Reset", inParams, Nothing)
                Return Convert.ToUInt32(outParams.Properties("ReturnValue").Value)
            Else
                Return Convert.ToUInt32(0)
            End If
        End Function

        Public Function SetPowerState(PowerState As UShort, Time As Date) As UInteger
            If isEmbedded = False Then
                Dim inParams As ManagementBaseObject = PrivateLateBoundObject.GetMethodParameters("SetPowerState")
                inParams("PowerState") = PowerState
                inParams("Time") = ToDmtfDateTime(Time)
                Dim outParams As ManagementBaseObject = PrivateLateBoundObject.InvokeMethod("SetPowerState", inParams, Nothing)
                Return Convert.ToUInt32(outParams.Properties("ReturnValue").Value)
            Else
                Return Convert.ToUInt32(0)
            End If
        End Function

        Public Enum AdapterTypeIdValues
            Ethernet_802_3 = 0
            Token_Ring_802_5 = 1
            Fiber_Distributed_Data_Interface_FDDI_ = 2
            Wide_Area_Network_WAN_ = 3
            LocalTalk = 4
            Ethernet_using_DIX_header_format = 5
            ARCNET = 6
            ARCNET_878_2_ = 7
            ATM = 8
            Wireless = 9
            Infrared_Wireless = 10
            Bpc = 11
            CoWan = 12
            Val_1394 = 13
            NULL_ENUM_VALUE = 14
        End Enum

        Public Enum AvailabilityValues
            Other0 = 1
            Unknown0 = 2
            Running_Full_Power = 3
            Warning = 4
            In_Test = 5
            Not_Applicable = 6
            Power_Off = 7
            Off_Line = 8
            Off_Duty = 9
            Degraded = 10
            Not_Installed = 11
            Install_Error = 12
            Power_Save_Unknown = 13
            Power_Save_Low_Power_Mode = 14
            Power_Save_Standby = 15
            Power_Cycle = 16
            Power_Save_Warning = 17
            Paused = 18
            Not_Ready = 19
            Not_Configured = 20
            Quiesced = 21
            NULL_ENUM_VALUE = 0
        End Enum

        Public Enum ConfigManagerErrorCodeValues
            This_device_is_working_properly_ = 0
            This_device_is_not_configured_correctly_ = 1
            Windows_cannot_load_the_driver_for_this_device_ = 2
            The_driver_for_this_device_might_be_corrupted_or_your_system_may_be_running_low_on_memory_or_other_resources_ = 3
            This_device_is_not_working_properly_One_of_its_drivers_or_your_registry_might_be_corrupted_ = 4
            The_driver_for_this_device_needs_a_resource_that_Windows_cannot_manage_ = 5
            The_boot_configuration_for_this_device_conflicts_with_other_devices_ = 6
            Cannot_filter_ = 7
            The_driver_loader_for_the_device_is_missing_ = 8
            This_device_is_not_working_properly_because_the_controlling_firmware_is_reporting_the_resources_for_the_device_incorrectly_ = 9
            This_device_cannot_start_ = 10
            This_device_failed_ = 11
            This_device_cannot_find_enough_free_resources_that_it_can_use_ = 12
            Windows_cannot_verify_this_device_s_resources_ = 13
            This_device_cannot_work_properly_until_you_restart_your_computer_ = 14
            This_device_is_not_working_properly_because_there_is_probably_a_re_enumeration_problem_ = 15
            Windows_cannot_identify_all_the_resources_this_device_uses_ = 16
            This_device_is_asking_for_an_unknown_resource_type_ = 17
            Reinstall_the_drivers_for_this_device_ = 18
            Failure_using_the_VxD_loader_ = 19
            Your_registry_might_be_corrupted_ = 20
            System_failure_Try_changing_the_driver_for_this_device_If_that_does_not_work_see_your_hardware_documentation_Windows_is_removing_this_device_ = 21
            This_device_is_disabled_ = 22
            System_failure_Try_changing_the_driver_for_this_device_If_that_doesn_t_work_see_your_hardware_documentation_ = 23
            This_device_is_not_present_is_not_working_properly_or_does_not_have_all_its_drivers_installed_ = 24
            Windows_is_still_setting_up_this_device_ = 25
            Windows_is_still_setting_up_this_device_0 = 26
            This_device_does_not_have_valid_log_configuration_ = 27
            The_drivers_for_this_device_are_not_installed_ = 28
            This_device_is_disabled_because_the_firmware_of_the_device_did_not_give_it_the_required_resources_ = 29
            This_device_is_using_an_Interrupt_Request_IRQ_resource_that_another_device_is_using_ = 30
            This_device_is_not_working_properly_because_Windows_cannot_load_the_drivers_required_for_this_device_ = 31
            NULL_ENUM_VALUE = 32
        End Enum

        Public Enum PowerManagementCapabilitiesValues
            Unknown0 = 0
            Not_Supported = 1
            Disabled = 2
            Enabled = 3
            Power_Saving_Modes_Entered_Automatically = 4
            Power_State_Settable = 5
            Power_Cycling_Supported = 6
            Timed_Power_On_Supported = 7
            NULL_ENUM_VALUE = 8
        End Enum

        Public Enum StatusInfoValues
            Other0 = 1
            Unknown0 = 2
            Enabled = 3
            Disabled = 4
            Not_Applicable = 5
            NULL_ENUM_VALUE = 0
        End Enum

        ' Enumerator implementation for enumerating instances of the class.
        Public Class NetworkAdapterCollection
            Inherits Object
            Implements ICollection

            Private ReadOnly privColObj As ManagementObjectCollection

            Public Sub New(objCollection As ManagementObjectCollection)
                privColObj = objCollection
            End Sub

            Public Overridable ReadOnly Property Count As Integer Implements ICollection.Count
                Get
                    Return privColObj.Count
                End Get
            End Property

            Public Overridable ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
                Get
                    Return privColObj.IsSynchronized
                End Get
            End Property

            Public Overridable ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
                Get
                    Return Me
                End Get
            End Property

            Public Overridable Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
                privColObj.CopyTo(array, index)
                Dim nCtr As Integer
                nCtr = 0

                While nCtr < array.Length
                    array.SetValue(New NetworkAdapter(CType(array.GetValue(nCtr), ManagementObject)), nCtr)
                    nCtr += 1
                End While
            End Sub

            Public Overridable Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
                Return New NetworkAdapterEnumerator(privColObj.GetEnumerator())
            End Function

            Public Class NetworkAdapterEnumerator
                Inherits Object
                Implements IEnumerator

                Private ReadOnly privObjEnum As ManagementObjectCollection.ManagementObjectEnumerator

                Public Sub New(objEnum As ManagementObjectCollection.ManagementObjectEnumerator)
                    privObjEnum = objEnum
                End Sub

                Public Overridable ReadOnly Property Current As Object Implements IEnumerator.Current
                    Get
                        Return New NetworkAdapter(CType(privObjEnum.Current, ManagementObject))
                    End Get
                End Property

                Public Overridable Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                    Return privObjEnum.MoveNext()
                End Function

                Public Overridable Sub Reset() Implements IEnumerator.Reset
                    privObjEnum.Reset()
                End Sub
            End Class
        End Class

        ' TypeConverter to handle null values for ValueType properties
        Public Class WMIValueTypeConverter
            Inherits TypeConverter

            Private ReadOnly baseConverter As TypeConverter
            Private ReadOnly baseType As Type

            Public Sub New(inBaseType As Type)
                baseConverter = TypeDescriptor.GetConverter(inBaseType)
                baseType = inBaseType
            End Sub

            Public Overrides Function CanConvertFrom(context As ITypeDescriptorContext, srcType As Type) As Boolean
                Return baseConverter.CanConvertFrom(context, srcType)
            End Function

            Public Overrides Function CanConvertTo(context As ITypeDescriptorContext, destinationType As Type) As Boolean
                Return baseConverter.CanConvertTo(context, destinationType)
            End Function

            Public Overrides Function ConvertFrom(context As ITypeDescriptorContext, culture As CultureInfo, value As Object) As Object
                Return baseConverter.ConvertFrom(context, culture, value)
            End Function

            Public Overrides Function CreateInstance(context As ITypeDescriptorContext, dictionary As IDictionary) As Object
                Return baseConverter.CreateInstance(context, dictionary)
            End Function

            Public Overrides Function GetCreateInstanceSupported(context As ITypeDescriptorContext) As Boolean
                Return baseConverter.GetCreateInstanceSupported(context)
            End Function

            Public Overrides Function GetProperties(context As ITypeDescriptorContext, value As Object, attributeVar As Attribute()) As PropertyDescriptorCollection
                Return baseConverter.GetProperties(context, value, attributeVar)
            End Function

            Public Overrides Function GetPropertiesSupported(context As ITypeDescriptorContext) As Boolean
                Return baseConverter.GetPropertiesSupported(context)
            End Function

            Public Overrides Function GetStandardValues(context As ITypeDescriptorContext) As StandardValuesCollection
                Return baseConverter.GetStandardValues(context)
            End Function

            Public Overrides Function GetStandardValuesExclusive(context As ITypeDescriptorContext) As Boolean
                Return baseConverter.GetStandardValuesExclusive(context)
            End Function

            Public Overrides Function GetStandardValuesSupported(context As ITypeDescriptorContext) As Boolean
                Return baseConverter.GetStandardValuesSupported(context)
            End Function

            Public Overrides Function ConvertTo(context As ITypeDescriptorContext, culture As CultureInfo, value As Object, destinationType As Type) As Object
                If baseType.BaseType Is GetType([Enum]) Then
                    If value.GetType() Is destinationType Then
                        Return value
                    End If
                    If value Is Nothing AndAlso context IsNot Nothing AndAlso context.PropertyDescriptor.ShouldSerializeValue(context.Instance) = False Then

                        Return "NULL_ENUM_VALUE"
                    End If
                    Return baseConverter.ConvertTo(context, culture, value, destinationType)
                End If
                If baseType Is GetType(Boolean) AndAlso baseType.BaseType Is GetType(ValueType) Then
                    If value Is Nothing AndAlso context IsNot Nothing AndAlso context.PropertyDescriptor.ShouldSerializeValue(context.Instance) = False Then
                        Return ""
                    End If
                    Return baseConverter.ConvertTo(context, culture, value, destinationType)
                End If
                If context IsNot Nothing AndAlso context.PropertyDescriptor.ShouldSerializeValue(context.Instance) = False Then
                    Return ""
                End If
                Return baseConverter.ConvertTo(context, culture, value, destinationType)
            End Function
        End Class

        ' Embedded class to represent WMI system Properties.
        <TypeConverter(GetType(ExpandableObjectConverter))>
        Public Class ManagementSystemProperties

            Private ReadOnly PrivateLateBoundObject As ManagementBaseObject

            Public Sub New(ManagedObject As ManagementBaseObject)
                PrivateLateBoundObject = ManagedObject
            End Sub

            <Browsable(True)>
            Public ReadOnly Property GENUS As Integer
                Get
                    Return CInt(PrivateLateBoundObject("__GENUS"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property [CLASS] As String
                Get
                    Return CStr(PrivateLateBoundObject("__CLASS"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property SUPERCLASS As String
                Get
                    Return CStr(PrivateLateBoundObject("__SUPERCLASS"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property DYNASTY As String
                Get
                    Return CStr(PrivateLateBoundObject("__DYNASTY"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property RELPATH As String
                Get
                    Return CStr(PrivateLateBoundObject("__RELPATH"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property PROPERTY_COUNT As Integer
                Get
                    Return CInt(PrivateLateBoundObject("__PROPERTY_COUNT"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property DERIVATION As String()
                Get
                    Return CType(PrivateLateBoundObject("__DERIVATION"), String())
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property SERVER As String
                Get
                    Return CStr(PrivateLateBoundObject("__SERVER"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property [NAMESPACE] As String
                Get
                    Return CStr(PrivateLateBoundObject("__NAMESPACE"))
                End Get
            End Property

            <Browsable(True)>
            Public ReadOnly Property PATH As String
                Get
                    Return CStr(PrivateLateBoundObject("__PATH"))
                End Get
            End Property
        End Class

    End Class

End Namespace
