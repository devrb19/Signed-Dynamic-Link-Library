# Signed-Dynamic-Link-Library

There are two different concepts in .NET:
Type	Purpose	Displays publisher identity?
Strong Name Signing	Gives a .NET assembly a unique identity using a key pair	No
Authenticode Signing	Digitally signs the DLL using a code-signing certificate	Yes

We need Authenticode signing because your concern is the Unknown Publisher message.

Microsoft also explicitly explains that strong names alone do not establish publisher trust.

--IMPORTANT-- 
For this local demonstration, we'll create a self-signed certificate. We'll also explicitly trust it on your development machine. This is suitable for learning, not production distribution. For production, you'll need a trusted organisational or publicly trusted code-signing certificate.

One important distinction: signing only the DLL does not automatically remove an Unknown Publisher warning for an unsigned EXE or installer. Those files need their own signatures too.
