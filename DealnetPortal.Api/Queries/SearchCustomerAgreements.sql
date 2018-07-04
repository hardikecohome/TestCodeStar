DECLARE @CONTRACT VARCHAR(10); 
DECLARE @LEASECOMPANY VARCHAR(10); 
DECLARE @CUST_ID VARCHAR(10); 
DECLARE @CUST_FIRST_NAME VARCHAR(50); 
DECLARE @CUST_LAST_NAME VARCHAR(50);
DECLARE @CUST_DATE_OF_BIRTH AS VARCHAR(20) ; 
DECLARE @CUST_COMPANY AS VARCHAR(10); 
DECLARE @F_PAY_DATE DATETIME; 
DECLARE @L_PAY_DATE DATETIME; 
DECLARE @TABLE TABLE ( 
       lease_num varchar(15), 
       lease_app_num varchar(15), 
       lease_book_type varchar(10), 
       lease_booked_post_date datetime, 
       lease_sign_date datetime, 
       lease_start_date datetime, 
       lease_maturity_date datetime, 
       lease_term varchar(4), 
       lease_type_desc varchar(20), 
       lease_type varchar(10), 
       lease_cust_id_num varchar(15), 
       equip_type varchar(5), 
       equip_type_desc varchar(30),
       equip_active_flag varchar(5) ) 
 
 SET @CUST_FIRST_NAME = '{0}' 
 SET @CUST_LAST_NAME = '{1}' 
 SET @CUST_DATE_OF_BIRTH = '{2}' 
 SET @CUST_COMPANY = '08' 
 DECLARE SMS_CURSOR CURSOR FOR 
 SELECT ROLECOMP.role_id AS cust_id_num 
 FROM LeasePlusTest.dbo.Entity entt 
 INNER JOIN LeasePlusTest.dbo.Role role ON Entt.oid = role.entt_oid AND role.role_type = 'CUST' 
 INNER JOIN LeasePlusTest.dbo.RoleCompany rolecomp ON role.oid = rolecomp.role_oid 
 INNER JOIN LeasePlusTest.dbo.Customer cust ON cust.oid = role.ref_oid and role.role_type = 'CUST' 
 LEFT JOIN LeasePlusTest.dbo.Entity parent ON parent.oid = entt.parent_oid 
 LEFT JOIN LeasePlusTest.dbo.Role parentrole ON parent.oid = parentrole.entt_oid AND parentrole.role_type = 'CUST' 
 LEFT JOIN LeasePlusTest.dbo.RoleCompany parentrolecomp ON parentrole.oid = parentrolecomp.role_oid 
 LEFT JOIN LeasePlusTest.dbo.Customer parentcust ON parentcust.oid = parentrole.ref_oid and parentrole.role_type = 'CUST' 
 LEFT JOIN LeasePlusTest.dbo.Contact cont ON cont.ref_oid = cust.oid AND cont.ref_type = 'CUST' AND cont.ctex_object_type = 'CUST' AND cont.usage_ind = 'P' 
 LEFT JOIN LeasePlusTest.dbo.IndustryType ityp ON ityp.industry_type = entt.ityp_industry_type 
 LEFT JOIN LeasePlusTest.dbo.LeaseSalesRep slsrep ON slsrep.SlsrepCompanyNum = RoleComp.comp_company_num AND slsrep.SlsrepIdNum = RoleComp.cust_slsrep_id_num 
 LEFT JOIN LeasePlusTest.dbo.Location loca ON loca.ref_oid = Cust.oid AND loca.ref_type = 'CUST' AND loca.location_code = 'DFT' 
 LEFT JOIN LeasePlusTest.dbo.Address addr ON addr.oid = loca.addr_oid 
 LEFT JOIN LeasePlusTest.dbo.AddressXref addx ON addx.addr_oid = addr.oid AND addx.ref_type = 'LOCA' AND addx.usage_ind = 'P' 
 LEFT JOIN LeasePlusTest.dbo.Location btloca ON btloca.ref_oid = Cust.oid AND btloca.ref_type = 'CUST' AND btloca.location_code = 'BIL' 
 LEFT JOIN LeasePlusTest.dbo.Address btaddr ON btaddr.oid = btloca.addr_oid 
 LEFT JOIN LeasePlusTest.dbo.AddressXref btaddx ON btaddx.addr_oid = addr.oid AND btaddx.ref_type = 'LOCA' AND btaddx.xref_type = 'B' AND btaddx.usage_ind = 'S' 
 LEFT JOIN LeasePlusTest.dbo.CompanyType comptype ON comptype.company_type = entt.comptype_company_type 
 LEFT JOIN LeasePlusTest.dbo.DataTable ON table_key = 'PERFORMANCE_STATUS' AND data_value = cust.performance_status 
 LEFT OUTER JOIN LeasePlusTest.dbo.LeaseControl cntrl ON cntrl.CntrlCompanyNum = rolecomp.comp_company_num where entt.fname = @CUST_FIRST_NAME and entt.lname = @CUST_LAST_NAME and 
 CONVERT(VARCHAR(20), entt.date_of_birth,101) = @CUST_DATE_OF_BIRTH 
 and ROLECOMP.comp_company_num = @CUST_COMPANY 
 order by cust_id_num desc 
 OPEN SMS_CURSOR FETCH NEXT FROM SMS_CURSOR 
 INTO @CUST_ID WHILE @@FETCH_STATUS = 0 
 BEGIN 
 PRINT @cust_id; 
 insert into @TABLE select lease_num, 
 lease_app_num, lease_book_type, 
 lease_booked_post_date, lease_sign_date,
  lease_start_date, lease_maturity_date,
   lease_term, lease_type_desc, lease_type,
    lease_cust_id_num, equip.equip_type, 
       equip.equip_type_desc, equip.equip_active_flag 
       from LeasePlusTest.dbo.LPlusLeaseVW 
       as lease 
       left join LeasePlusTest.dbo.[LPlusEquipmentVW] as equip 
       on lease.lease_comp_num = equip.equip_comp_num and lease.lease_cust_id_num = equip.equip_cust_id_num and lease.lease_num = equip.equip_lease_num 
       where lease_cust_id_num = @CUST_ID and lease.lease_term_date is null 
       FETCH NEXT FROM SMS_CURSOR INTO @CUST_ID 
       END 
       CLOSE SMS_CURSOR 
       DEALLOCATE SMS_CURSOR 
       select * from @TABLE 
       delete from @TABLE
