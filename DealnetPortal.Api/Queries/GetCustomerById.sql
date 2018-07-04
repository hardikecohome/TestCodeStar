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
                                        where e.entt_id = '{0}'
                                        and e.fname is not null
                                        and e.lname is not null;
