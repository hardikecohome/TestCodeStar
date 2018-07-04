SELECT 
DISTINCT

	COALESCE(parent_d_type.value,d_type.value) as Product_Type,
	COALESCE(parent_c_type.value,c_type.value) as Channel_Type,
	case when COALESCE(parent_ratecard.Value, ratecard.value) is null then 'Tier 1'  when COALESCE(parent_ratecard.Value, ratecard.value) = 'Rate Card Tier 2' then 'Tier 1' when
	COALESCE(parent_ratecard.Value, ratecard.value) = 'Rate Card Tier 1' then 'Tier 1'
	 else COALESCE(parent_ratecard.Value, ratecard.value) end as ratecard,
	 	 
	case when COALESCE(parent_leaseratecard.Value, leaseratecard.value) is null then ''  when COALESCE(parent_leaseratecard.Value, leaseratecard.value) = 'Rate Card Tier 2' then 'Tier 1' when
	COALESCE(parent_leaseratecard.Value, leaseratecard.value) = 'Rate Card Tier 1' then 'Tier 1'
	 else COALESCE(parent_leaseratecard.Value, leaseratecard.value) end as lease_ratecard,

	rol.active,
	rol.inactive_date,
	rol.descr as Role,
	sc.user_id,

	e.entt_id,e.oid, 
	COALESCE(p.name,e.name) as name,
	COALESCE(p.name,COALESCE(e.name,e.fname)) as fname,
	e.lname, e.date_of_birth, ltrim(rtrim(case when charindex('|',replace(replace(e.email_addr,';','|'),',','|')) > 0 then  substring(e.email_addr,1,charindex('|',replace(replace(e.email_addr,';','|'),',','|'))-1) when CHARINDEX(',',e.email_addr)> 0 then substring(e.email_addr,1,charindex('|',replace(replace(e.email_addr,';','|'),',','|'))) else e.email_addr end)) as email_addr, 
    l.city, l.state, l.postal_code, l.addr_line1,												 
    main.phone_num AS phone_num, fax.phone_num as Fax,
	e.parent_oid,												 
	p.name as parent_name,
	scparent.user_id as parent_uname
FROM Entity (nolock) as e
INNER JOIN SecurityUser (nolock) sc on sc.oid = e.secu_oid
LEFT JOIN Location (nolock) as l
    on (e.oid = l.entt_oid and e.loca_oid = l.oid)
LEFT JOIN Phone (nolock) as main
    on (e.oid = main.entt_oid and main.phone_type ='M') 
LEFT JOIN Phone (nolock) as fax
    on (e.oid = fax.entt_oid and fax.phone_type ='F')
 LEFT JOIN Entity (nolock) as p 	on e.parent_oid = p.oid  
 LEFT JOIN SecurityUser (nolock) scparent on scparent.oid = P.secu_oid

 LEFT JOIN [dbo].[Role] (NOLOCK) AS rol on rol.[oid] = sc.contract_attach_role_oid
 LEFT JOIN [DocGenAccOtherUDF-Dealer Type] d_type (NOLOCK) ON e.oid = d_type.oid
 LEFT JOIN [DocGenAccOtherUDF-ChannelType] c_type (NOLOCK) ON e.oid = c_type.oid
 LEFT JOIN [DocGenAccOtherUDF-RateCard] ratecard (NOLOCK) ON e.oid = ratecard.oid
 LEFT JOIN [DocGenAccOtherUDF-leaseratecard] leaseratecard (NOLOCK) ON e.oid = leaseratecard.oid
 
 LEFT JOIN [DocGenAccOtherUDF-RateCard] parent_ratecard (NOLOCK) ON e.parent_oid = parent_ratecard.oid
 LEFT JOIN [DocGenAccOtherUDF-Dealer Type] parent_d_type (NOLOCK) ON e.parent_oid = parent_d_type.oid
 LEFT JOIN [DocGenAccOtherUDF-ChannelType] parent_c_type (NOLOCK) ON e.parent_oid = parent_c_type.oid
 LEFT JOIN [DocGenAccOtherUDF-leaseratecard] parent_leaseratecard (NOLOCK) ON e.oid = parent_leaseratecard.oid
 
 where sc.user_id  = '{0}';