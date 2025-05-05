using DogTracker.Data;
using DogTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class ContactService(AppDbContext context, ILogger<ContactService> logger)
{
    public async Task<List<Contact>> GetContactsAsync(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération de tous les contacts pour le chien {DogId}", dogId);
        
            var contacts = await context.Contacts
                .Where(c => c.DogId == dogId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        
            logger.LogInformation("Contacts récupérés avec succès pour le chien {DogId}", dogId);
            return contacts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des contacts pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les contacts.", ex);
        }
    }
    
    public async Task<Contact> GetContactAsync(int contactId)
    {
        try
        {
            logger.LogInformation("Récupération du contact {ContactId}", contactId);
            var contact = await context.Contacts
                .FirstOrDefaultAsync(c => c.Id == contactId);
            
            if (contact == null)
            {
                logger.LogWarning("Aucun contact trouvé avec l'ID {ContactId}", contactId);
                throw new KeyNotFoundException($"Aucun contact trouvé avec l'ID {contactId}");
            }
            
            logger.LogInformation("Contact récupéré avec succès : {Contact}", contact);
            return contact;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du contact {ContactId}", contactId);
            throw new ApplicationException("Impossible de récupérer le contact.", ex);
        }
    }
    
    public async Task AddContactAsync(int dogId, Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        try
        {
            contact.DogId = dogId;
            context.Contacts.Add(contact);
            await context.SaveChangesAsync();
            logger.LogInformation("Contact ajouté avec succès pour le chien {DogId}", dogId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'ajout du contact pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'ajouter le contact.", ex);
        }
    }
    
    public async Task UpdateContactAsync(Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        try
        {
            context.Contacts.Update(contact);
            await context.SaveChangesAsync();
            logger.LogInformation("Contact mis à jour avec succès : {Contact}", contact);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour du contact : {Contact}", contact);
            throw new ApplicationException("Impossible de mettre à jour le contact.", ex);
        }
    }
    
    public async Task DeleteContactAsync(int contactId)
    {
        try
        {
            logger.LogInformation("Suppression du contact {ContactId}", contactId);
            var contact = await context.Contacts.FindAsync(contactId);
            if (contact != null)
            {
                context.Contacts.Remove(contact);
                await context.SaveChangesAsync();
                logger.LogInformation("Contact supprimé avec succès : {ContactId}", contactId);
            }
            else
            {
                logger.LogWarning("Aucun contact trouvé avec l'ID {ContactId}", contactId);
                throw new KeyNotFoundException($"Aucun contact trouvé avec l'ID {contactId}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du contact {ContactId}", contactId);
            throw new ApplicationException("Impossible de supprimer le contact.", ex);
        }
    }
    
    public async Task<List<Contact>> GetContactsByTypeAsync(int dogId, string type)
    {
        try
        {
            logger.LogInformation("Récupération des contacts de type {Type} pour le chien {DogId}", type, dogId);
        
            var contacts = await context.Contacts
                .Where(c => c.DogId == dogId && c.Type == type)
                .OrderBy(c => c.Name)
                .ToListAsync();
        
            logger.LogInformation("Contacts de type {Type} récupérés avec succès pour le chien {DogId}", type, dogId);
            return contacts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des contacts de type {Type} pour le chien {DogId}", type, dogId);
            throw new ApplicationException("Impossible de récupérer les contacts.", ex);
        }
    }
    
}