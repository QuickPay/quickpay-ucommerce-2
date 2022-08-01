Umbraco Create nuget package.

	./nuget.exe pack UCommerce.Transactions.Payments.Unzer.csproj -version 1.0.0


The following values are set in the .nuspec file.

 - Auther 
 - Description
 - ReleadeNotes
 - Copyright
 - Tags

The Version number can be set / changed with the cli command "-version x.x.x" at the end of the command to create the package, like below:

	./nuget.exe pack .\UCommerce.Transactions.Payments.Quickpay.csproj.nuspec -version 1.0.0