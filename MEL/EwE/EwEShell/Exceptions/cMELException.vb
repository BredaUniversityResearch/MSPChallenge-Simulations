﻿' ===============================================================================
' This file is part of Ecopath with Ecosim (EwE)
'
' EwE is free software: you can redistribute it and/or modify it under the terms
' of the GNU General Public License version 2 as published by the Free Software 
' Foundation.
'
' EwE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
' without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
' PURPOSE. See the GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License along with EwE.
' If not, see <http://www.gnu.org/licenses/gpl-2.0.html>. 
'
' Copyright 2016- 
'    Ecopath International Initiative, Barcelona, Spain
' ===============================================================================
'

''' ---------------------------------------------------------------------------
''' <summary>
''' Boink.
''' </summary>
''' ---------------------------------------------------------------------------
Public Class cMELException
    Inherits Exception

    ''' -----------------------------------------------------------------------
    ''' <summary>
    ''' Create a new <see cref="cMELException"/>.
    ''' </summary>
    ''' <param name="strDetails">The exception details.</param>
    ''' -----------------------------------------------------------------------
    Public Sub New(strDetails As String)
        MyBase.New(strDetails)
        Debug.WriteLine("MEL Exception: " & strDetails)
    End Sub

End Class
