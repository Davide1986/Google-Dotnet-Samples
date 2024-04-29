
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Exception = System.Exception;

namespace ConsoleGooglePeople
{
    class Program
    {
        private static string[] Scopes = { PeopleServiceService.Scope.DirectoryReadonly, PeopleServiceService.Scope.Contacts ,PeopleServiceService.Scope.ContactsReadonly };
        private static string ApplicationName = "SyncGoogleContacts";
        private static string TokenDirectory = "tokens";

        private static List<UserCredential> AuthenticateGmailAccounts(List<string> accounts)
        {
            var credentials = new List<UserCredential>();
            Directory.CreateDirectory(TokenDirectory); // Assicurarsi che la directory esista

            foreach (var account in accounts)
            {
                var credPath = Path.Combine(TokenDirectory, $"{account}.json");
                UserCredential credential;

                using (var stream = new FileStream(@"C:\client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        account,
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }

                credentials.Add(credential);
            }

            return credentials;
        }

        private static UserCredential GetAuthenticateGmailAccount(string account)
        {
            Directory.CreateDirectory(TokenDirectory); // Assicurarsi che la directory esista

            var credPath = Path.Combine(TokenDirectory, $"{account}.json");
            UserCredential credential;

            try
            {
                // Tenta di caricare le credenziali
                using (var stream = new FileStream(@"C:\client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        account,
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }
            }
            catch (Exception)
            {
                // Se non ci sono credenziali, crea un nuovo set
                using (var stream = new FileStream(@"C:\client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        account,
                        CancellationToken.None,
                        new FileDataStore(TokenDirectory, true)).Result;
                }
            }

            return credential;
        }

        private static List<Person> GetGmailContacts(PeopleServiceService service)
        {
            try
            {
                var request = service.People.Connections.List("people/me");
               // request.PersonFields = "names,emailAddresses";
                request.RequestMaskIncludeField = new List<string>() {
              "person.phoneNumbers" ,
              "person.EmailAddresses",
              "person.names"
              };
                var response = request.Execute();

                if (response.Connections == null)
                {
                    return new List<Person>(); // Restituisce una lista vuota se non ci sono contatti
                }

                return (List<Person>)response.Connections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during contact retrieval: {ex.Message}"); // Gestione dell'errore
                return new List<Person>(); // Restituisce una lista vuota in caso di errore
            }
        }

        private static List<Person> MergeContacts(List<List<Person>> allContacts)
        {
            var uniqueContacts = new Dictionary<string, Person>();

            foreach (var contactList in allContacts)
            {
                if (contactList == null)
                {
                    continue; // Se la lista è null, passa alla prossima
                }

                foreach (var person in contactList)
                {
                    if (person == null)
                    {
                        continue; // Salta i contatti null
                    }

                    var email = person.EmailAddresses?[0]?.Value ?? "";
                    if (!string.IsNullOrEmpty(email))
                    {
                        if (!uniqueContacts.ContainsKey(email))
                        {
                          //  Console.WriteLine(email.ToString());
                         
                     
                                    Console.WriteLine(person.Names != null ? (person.Names[0].DisplayName + "  " ?? "n/a") : "n/a  ");
                                    Console.WriteLine(person.EmailAddresses?.FirstOrDefault()?.Value + "  ");
                                    Console.WriteLine(person.PhoneNumbers?.FirstOrDefault()?.Value);
                            


                                uniqueContacts[email] = person; // Aggiungi il contatto solo se l'email non è vuota
                        }
                    }
                }
            }

            return new List<Person>(uniqueContacts.Values);
        }



        public static async Task Main(string[] args)
        {
            try
            {
                var gmailAccounts = new List<string> { "dmeo1@gmail.com", "demo2@gmail.com" };
                var credentials = AuthenticateGmailAccounts(gmailAccounts);

                TaskMerge(credentials);

                Person contactToCreate = new Person
                {
                    Names = new List<Name> { new Name { GivenName = "Name", FamilyName = "Surname" } },
                    EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress { Value = "email11@example.com" },
                new EmailAddress { Value = "email12@example.com" }
            },
                    PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber { Value = "44444-222", Type = "work" },
                new PhoneNumber { Value = "+39 1111 333", Type = "mobile" }
            },
                    Organizations = new List<Organization>
            {
                new Organization { Name = "Company Srl" }
            }
                };


                var serviceGetAccount = GetServices("dmeo1@gmail.com");
                await AddContactAsync(serviceGetAccount, contactToCreate);

                contactToCreate = new Person
                {
                    Names = new List<Name> { new Name { GivenName = "Name2", FamilyName = "Surname2" } },
                    EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress { Value = "dmeo21@gmail.com" },
                new EmailAddress { Value = "dmeo22@gmail.com" }
            },
                    PhoneNumbers = new List<PhoneNumber>
            {
                new PhoneNumber { Value = "88888-666", Type = "work" },
                new PhoneNumber { Value = "+39 1111 333", Type = "mobile" }
            },
                    Organizations = new List<Organization>
            {
                new Organization { Name = "CompanyShop Srl" }
            }
                };

                serviceGetAccount = GetServices("demo2@gmail.com");
                await AddContactAsync(serviceGetAccount, contactToCreate);

                await DeleteContactByEmailAsync(serviceGetAccount, "roma.giovaeeeeeni@example.com");
                await UpdateContactByEmailAsync(serviceGetAccount, "dmeo22@gmail.com");


                TaskMerge(credentials);

                //var serviceGetAccount = GetServices("dmeo22@gmail.com");
               

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}"); // Gestione delle eccezioni generali
            }
        }

        public static void TaskMerge(List<UserCredential> credentials)
        {
            var allContacts = new List<List<Person>>();
            foreach (var credential in credentials)
            {
                var service = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                var contacts = GetGmailContacts(service); // Richiama il metodo modificato con try-catch
                allContacts.Add(contacts);

            }

            var mergedContacts = MergeContacts(allContacts);

            Console.WriteLine("Contacts merged successfully!");
            Console.WriteLine($"Total unique contacts: {mergedContacts.Count}");
        }

        private static PeopleServiceService GetServices(string accountGmailAccounts)
        {

            var credential = GetAuthenticateGmailAccount(accountGmailAccounts);
            var service = new PeopleServiceService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }



        public static async Task UpdateContactByEmailAsync(PeopleServiceService service, string emailAddress)
        {
            try
            {
                // Effettua una ricerca per l'indirizzo email
                var contactRequest = service.People.Connections.List("people/me");
                contactRequest.RequestMaskIncludeField = new List<string>() {
              "person.phoneNumbers" ,
              "person.EmailAddresses",
              "person.names"
              };
                var response = await contactRequest.ExecuteAsync();

                if (response?.Connections != null)
                {
                    foreach (var person in response.Connections)
                    {
                        if (person.EmailAddresses != null)
                        {
                            foreach (var email in person.EmailAddresses)
                            {
                                if (email.Value == emailAddress)
                                {
                                    Person persons = person;
                                    persons.Names = new List<Name> { new Name { GivenName = "Giorgio", FamilyName = "Fontana" } };
                                    persons.EmailAddresses = new List<EmailAddress>
                                        {
                                            new EmailAddress { Value = "apollo@example.com" },
                                            new EmailAddress { Value = "raffello@example.com" }
                                        };
                                    persons.Organizations = new List<Organization>
                                        {
                                            new Organization { Name = "DDR Srl" }
                                        };

                                    // Trovato il contatto, elimina il contatto utilizzando l'ID del contatto
                                    var updateRequest = service.People.UpdateContact(persons, person.ResourceName);
                                    updateRequest.UpdatePersonFields = "names,emailAddresses,organizations";
                                    await updateRequest.ExecuteAsync();

                                    Console.WriteLine($"Modificato contatto con l'indirizzo email '{emailAddress}' modificato con successo.");
                                    return;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Nessun contatto trovato con l'indirizzo email '{emailAddress}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'eliminazione del contatto: {ex.Message}");
            }
        }


        public static async Task DeleteContactByEmailAsync(PeopleServiceService service, string emailAddress)
        {
            try
            {
                // Effettua una ricerca per l'indirizzo email
                var contactRequest = service.People.Connections.List("people/me");
                contactRequest.RequestMaskIncludeField = "person.emailAddresses";
                var response = await contactRequest.ExecuteAsync();

                if (response?.Connections != null)
                {
                    foreach (var person in response.Connections)
                    {
                        if (person.EmailAddresses != null)
                        {
                            foreach (var email in person.EmailAddresses)
                            {
                                if (email.Value == emailAddress)
                                {
                                    // Trovato il contatto, elimina il contatto utilizzando l'ID del contatto
                                    await service.People.DeleteContact(person.ResourceName).ExecuteAsync();
                                    Console.WriteLine($"Contatto con l'indirizzo email '{emailAddress}' eliminato con successo.");
                                    return;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Nessun contatto trovato con l'indirizzo email '{emailAddress}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'eliminazione del contatto: {ex.Message}");
            }
        }

        public static async Task AddContactAsync(PeopleServiceService service, Person contactToCreate)
        {                

            try
            {
                // Create the contact
                Person createdContact = await service.People.CreateContact(contactToCreate).ExecuteAsync();
                Console.WriteLine("Contatto creato con successo: " + createdContact.ResourceName);

                await Task.Delay(TimeSpan.FromSeconds(10)); // Attendi 10 secondi

            }
            catch (Exception e)
            {
                Console.WriteLine("Errore durante la creazione del contatto: " + e.Message);
            }
        }



    } //end clas

} //end 


