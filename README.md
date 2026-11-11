# Signed-Dynamic-Link-Library

There are two different concepts in .NET:
Type	Purpose	Displays publisher identity?
Strong Name Signing	Gives a .NET assembly a unique identity using a key pair	No
Authenticode Signing	Digitally signs the DLL using a code-signing certificate	Yes

We need Authenticode signing because your concern is the Unknown Publisher message.

Microsoft also explicitly explains that strong names alone do not establish publisher trust.
