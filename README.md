# RestXUnitTests

## Overview
This test suite runs the tests as defined in the **_RestApi.xlsx_** spreadsheet in the **_/src/testfiles_** directory. Output is handled through xUnit and will report tests properly in ADO when run in a build or release pipeline, as well as the command line.<br/>
More information is included in the _Overview_ worksheet in the spreadsheet file. Note that this framework does not require any Microsoft Office libraries to run.

## Service Under Test  
The API under test is intended to be used for the maintenance of Stock Keeping Unit identifiers (SKUs) which are used to identify and track the items the company has for sale.

The following CRUD operations are tested.


 - Create and Update operations through HTTP POSTs:  
<br/>
<code>
&nbsp;&nbsp;&nbsp;POST https://1ryu4whyek.execute-api.us-west-2.amazonaws.com/dev/skus
</code><br/>
Where the post body contains a SKU, Description and Price.<br/><br/>
<code>
<br/>
&nbsp;&nbsp;&nbsp;{<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"sku":"berliner", <br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"description": "Jelly donut", <br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"price":"2.99"<br/>
&nbsp;&nbsp;&nbsp;}<br/>
</code>  
<br/>
 - Read operations through HTTP GETs  <br/>
<code><br/>
&nbsp;&nbsp;&nbsp;GET https://1ryu4whyek.execute-api.us-west-2.amazonaws.com/dev/skus <br/>
&nbsp;&nbsp;&nbsp;GET https://1ryu4whyek.execute-api.us-west-2.amazonaws.com/dev/skus/{id}<br/>
</code><br/>
 - Delete operations are through HTTP DELETEs  <br/>
<code><br/>
&nbsp;&nbsp;&nbsp;DELETE https://1ryu4whyek.execute-api.us-west-2.amazonaws.com/dev/skus/{id} 
</code>
<br/>

## Running  
To run the tests from the command line use  
<br/>
<code>&nbsp;&nbsp;&nbsp;dotnet test</code>  
from the solution directory.

This framework is tested on dotnet version 5.0.102 (the project uses dotnet core 3.1).
To find your dotnet version use dotnet --version




