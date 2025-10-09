Imports System.Collections
Imports System.Reflection
Imports System.Linq

''' <summary>
''' A class to access the dump of variables of an object.
''' </summary>
Public Class ObjectDump

    Public ReadOnly Property Dump As String = ""

    Public Sub New(ByVal sender As Object)
        If sender Is Nothing Then
            Dump = "Object reference not set to an instance of an object."
        Else
            Dim t = sender.GetType()
            Dim fields() As FieldInfo = t.GetFields(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static)
            Dim properties() As PropertyInfo = t.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static)

            Dump =
                "--------------------------------------------------" & Environment.NewLine &
                "Generated Fields:" & Environment.NewLine &
                "--------------------------------------------------" & Environment.NewLine

            For Each field As FieldInfo In fields
                If Dump <> "" Then Dump &= Environment.NewLine

                Dim fieldAccessToken As String = ""
                If field.IsPublic Then
                    fieldAccessToken = "Public "
                ElseIf field.IsPrivate Then
                    fieldAccessToken = "Private "
                ElseIf field.IsFamily Then
                    fieldAccessToken = "Protected "
                End If
                If field.IsStatic Then fieldAccessToken &= "Shared "

                Dim fieldNameToken As String = field.Name
                Dim fieldTypeToken As String = field.FieldType.Name
                Dim fieldValueToken As String

                Try
                    Dim val = field.GetValue(sender)
                    If field.FieldType.IsArray Then
                        fieldValueToken = DumpArray(val)
                    ElseIf field.FieldType.IsGenericType Then
                        If field.FieldType.Name = "List`1" Then
                            fieldTypeToken = "List(Of " & field.FieldType.GetGenericArguments()(0).Name & ")"
                            fieldValueToken = DumpGenericArray(val, "List`1")
                        ElseIf field.FieldType.Name = "Dictionary`2" Then
                            fieldTypeToken = "Dictionary(Of " & field.FieldType.GetGenericArguments()(0).Name & ", " & field.FieldType.GetGenericArguments()(1).Name & ")"
                            fieldValueToken = DumpGenericArray(val, "Dictionary`2")
                        Else
                            fieldValueToken = DumpObject(val)
                        End If
                    ElseIf field.FieldType.Name = "Texture2D" Then
                        fieldValueToken = DumpTexture2D(val)
                    Else
                        fieldValueToken = DumpObject(val)
                    End If
                Catch ex As Exception
                    fieldValueToken = "<error: " & ex.Message & ">"
                End Try

                Dump &= fieldAccessToken & fieldNameToken & " As " & fieldTypeToken & " = " & fieldValueToken
            Next

            Dump &= Environment.NewLine & Environment.NewLine &
                "--------------------------------------------------" & Environment.NewLine &
                "Generated Property:" & Environment.NewLine &
                "--------------------------------------------------" & Environment.NewLine

            For Each [property] As PropertyInfo In properties
                ' Skip indexer/default properties (have parameters)
                If [property].GetIndexParameters().Length > 0 Then Continue For
                If Not [property].CanRead Then Continue For

                If Dump <> "" Then Dump &= Environment.NewLine

                Dim propertyNameToken As String = [property].Name
                Dim propertyTypeToken As String = [property].PropertyType.Name
                Dim propertyValueToken As String

                Try
                    Dim pType = [property].PropertyType
                    Dim pVal As Object = [property].GetValue(sender, Nothing)

                    If pType.IsArray Then
                        propertyValueToken = DumpArray(pVal)
                    ElseIf pType.IsGenericType Then
                        If pType.Name = "List`1" Then
                            propertyTypeToken = "List(Of " & pType.GetGenericArguments()(0).Name & ")"
                            propertyValueToken = DumpGenericArray(pVal, "List`1")
                        ElseIf pType.Name = "Dictionary`2" Then
                            propertyTypeToken = "Dictionary(Of " & pType.GetGenericArguments()(0).Name & ", " & pType.GetGenericArguments()(1).Name & ")"
                            propertyValueToken = DumpGenericArray(pVal, "Dictionary`2")
                        Else
                            propertyValueToken = DumpObject(pVal)
                        End If
                    ElseIf pType.Name = "Texture2D" Then
                        propertyValueToken = DumpTexture2D(pVal)
                    Else
                        propertyValueToken = DumpObject(pVal)
                    End If
                Catch ex As TargetInvocationException
                    Dim msg As String = If(ex.InnerException IsNot Nothing, ex.InnerException.Message, ex.Message)
                    propertyValueToken = "<error: " & msg & ">"
                Catch ex As Exception
                    propertyValueToken = "<error: " & ex.Message & ">"
                End Try

                Dump &= "Property " & propertyNameToken & " As " & propertyTypeToken & " = " & propertyValueToken
            Next
        End If
    End Sub

    Private Function DumpArray(ByVal obj As Object) As String
        Try
            If obj IsNot Nothing Then
                Dim listValue As Array = CType(obj, Array)
                If listValue.Length = 0 Then
                    Return "{}"
                Else
                    Return "{" & String.Join(", ", listValue.Cast(Of Object).Select(Function(a) If(a Is Nothing, "Nothing", a.ToString())).ToArray()) & "}"
                End If
            Else
                Return "Nothing"
            End If
        Catch ex As Exception
            Return "Array too complex to dump."
        End Try
    End Function

    Private Function DumpGenericArray(ByVal obj As Object, ByVal genericType As String) As String
        Try
            If obj Is Nothing Then Return "Nothing"

            If genericType = "List`1" Then
                Dim toArrayMethod = obj.GetType().GetMethod("ToArray")
                Dim arr As Array = CType(toArrayMethod.Invoke(obj, Nothing), Array)
                If arr.Length = 0 Then Return "{}"
                Return "{" & String.Join(", ", arr.Cast(Of Object).Select(Function(a) If(a Is Nothing, "Nothing", a.ToString())).ToArray()) & "}"
            ElseIf genericType = "Dictionary`2" Then
                Dim keysEnum As IEnumerable = CType(obj.GetType().GetProperty("Keys").GetValue(obj, Nothing), IEnumerable)
                Dim valsEnum As IEnumerable = CType(obj.GetType().GetProperty("Values").GetValue(obj, Nothing), IEnumerable)
                Dim dictionaryKeys As Object() = If(keysEnum Is Nothing, New Object() {}, keysEnum.Cast(Of Object)().ToArray())
                Dim dictionaryValues As Object() = If(valsEnum Is Nothing, New Object() {}, valsEnum.Cast(Of Object)().ToArray())

                If dictionaryKeys.Length = 0 OrElse dictionaryValues.Length = 0 Then Return "{}"
                Dim parts As New List(Of String)(Math.Min(dictionaryKeys.Length, dictionaryValues.Length))
                For i As Integer = 0 To Math.Min(dictionaryKeys.Length, dictionaryValues.Length) - 1
                    Dim k As String = If(dictionaryKeys(i) Is Nothing, "Nothing", dictionaryKeys(i).ToString())
                    Dim v As String = If(dictionaryValues(i) Is Nothing, "Nothing", dictionaryValues(i).ToString())
                    parts.Add("{" & k & ", " & v & "}")
                Next
                Return "{" & String.Join(", ", parts) & "}"
            Else
                Return "Generic Type too complex to dump."
            End If
        Catch ex As Exception
            Return "Generic Type too complex to dump."
        End Try
    End Function

    Private Function DumpTexture2D(ByVal obj As Object) As String
        Try
            If obj Is Nothing Then Return "Nothing"

            Dim widthProp = obj.GetType().GetProperty("Width")
            Dim heightProp = obj.GetType().GetProperty("Height")
            Dim nameProp = obj.GetType().GetProperty("Name")

            Dim width As Integer = 0
            Dim height As Integer = 0
            If widthProp IsNot Nothing Then width = Convert.ToInt32(widthProp.GetValue(obj, Nothing))
            If heightProp IsNot Nothing Then height = Convert.ToInt32(heightProp.GetValue(obj, Nothing))

            Dim rawName As Object = Nothing
            If nameProp IsNot Nothing Then rawName = nameProp.GetValue(obj, Nothing)
            Dim nameStr As String
            If rawName Is Nothing OrElse String.IsNullOrEmpty(rawName.ToString()) Then
                nameStr = """"""
            Else
                nameStr = rawName.ToString()
            End If

            Return "{Name = " & nameStr & ", Width = " & width.ToString() & ", Height = " & height.ToString() & "}"
        Catch ex As Exception
            Return "Texture2D too complex to dump."
        End Try
    End Function

    Private Function DumpObject(ByVal obj As Object) As String
        Try
            If obj Is Nothing Then Return "Nothing"
            Dim s As String = obj.ToString()
            If String.IsNullOrEmpty(s) Then
                Return """"""""
            Else
                Return s
            End If
        Catch ex As Exception
            Return "Object too complex to dump."
        End Try
    End Function

End Class