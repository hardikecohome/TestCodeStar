SELECT e.entt_id, e.fname, e.lname, e.date_of_birth, e.email_addr, 
		                                    l.postal_code, l.city, l.state, l.postal_code, l.addr_line1,
		                                    p.phone_num, ls.Value as leaseSource
                                    FROM Entity (nolock) as e
                                    LEFT JOIN Location (nolock) as l
                                        on (e.oid = l.entt_oid and e.loca_oid = l.oid)
                                    LEFT JOIN Phone (nolock) as p
                                        on (e.oid = p.entt_oid)
									LEFT JOIN  [DocGenAccCustUDF-Lead Source] as ls (nolock)
                                         on ls.oid = e.oid
                                    where   e.fname LIKE '{0}%'
                                        and e.lname LIKE '{1}%'
                                        and   convert (varchar(10),e.date_of_birth,101) = LEFT('{2}',10)
                                        and REPLACE(l.postal_code,' ','') LIKE '{3}%';