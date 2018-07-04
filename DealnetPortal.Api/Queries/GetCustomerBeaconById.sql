SELECT distinct top 1 

	 rptent.fname 
	,rptent.lname 
	,cast(rptent.entt_id as varchar(10)) as customer_id  
    ,rptent.date_of_birth 
	,cast(RPT.ContractOid as varchar(10)) as contractoid  
	,rpt.request_date as crtd_datetime  
	,rpt.LastChangeOperator as lupd_user 
	,rpt.LastChangeDateTime as lupd_datetime  
	
	,cast(rpt.oid as varchar(10)) as creditoid  
	
	,COALESCE ( CASE WHEN CAST(ISNULL(Rpt.xml_report.query('data(/XML_INTERFACE/CREDITREPORT/OBJECTS/CCSUBJECT/CCSCORES/ITEM_SCORE/SCORE)'), '') AS VARCHAR(max)) = 0 
			THEN NULL ELSE 
			CAST(ISNULL(Rpt.xml_report.query('data(/XML_INTERFACE/CREDITREPORT/OBJECTS/CCSUBJECT/CCSCORES/ITEM_SCORE/SCORE)'), '') AS VARCHAR(max))
         
			END 
			, CU.Text ) AS   Beacon 

	FROM [dbo].CreditReport (nolock)Rpt  
		LEFT JOIN [dbo].Entity (nolock) RptEnt   			ON Rpt.entt_oid = RptEnt.oid  
     	LEFT JOIN [dbo].CreditBureau (nolock) CB   			on Rpt.Bureau_oid = CB.Oid  
		LEFT JOIN [dbo].[DocGenAccCustUDF-BEACON]  (nolock) CU  on RptEnt.oid = cu.oid
where rptent.entt_id = '{0}'
order by lupd_datetime desc

