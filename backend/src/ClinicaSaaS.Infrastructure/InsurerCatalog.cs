using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure;

public static class InsurerCatalog
{
    private static readonly string[] Names =
    [
        "Accord Salud",
        "APROSS",
        "Avalian",
        "DOSUBA",
        "Galeno",
        "Hospital Alemán",
        "Hospital Británico",
        "Hospital Italiano",
        "IAPOS",
        "IOMA",
        "IOSPER",
        "IPROSS",
        "ISSN",
        "Jerárquicos Salud",
        "Luis Pasteur",
        "Medicus",
        "Medifé",
        "ObSBA",
        "Omint",
        "OSDE",
        "OSECAC",
        "OSDEPYM",
        "OSDOP",
        "OSEP",
        "OSFATLyF",
        "OSFATUN",
        "OSMATA",
        "OSPEDYC",
        "OSPE",
        "OSPF",
        "OSPJN",
        "OSPLAD",
        "OSPOCE",
        "OSPRERA",
        "OSSEG",
        "OSUTHGRA",
        "PAMI",
        "Premedic",
        "Prevención Salud",
        "Sancor Salud",
        "SEROS",
        "Swiss Medical",
        "Unión Personal",
        "William Hope"
    ];

    private static readonly string[] Starter =
    [
        "Galeno",
        "IOMA",
        "Medicus",
        "OSDE",
        "PAMI",
        "Swiss Medical"
    ];

    public static async Task EnsureAsync(TenantDbContext db, CancellationToken ct)
    {
        if (await db.Insurers.AnyAsync(ct))
            return;

        var insurers = Names.Select(name => new Insurer { Id = Guid.NewGuid(), Name = name, Enabled = true }).ToList();
        db.Insurers.AddRange(insurers);
        var starter = insurers.Where(i => Starter.Contains(i.Name)).ToList();
        var doctors = await db.Employees.Include(e => e.User).Where(e => e.User.Role == TenantRole.Doctor).ToListAsync(ct);
        foreach (var doctor in doctors)
        {
            foreach (var insurer in starter)
                doctor.Insurers.Add(insurer);
        }

        await db.SaveChangesAsync(ct);
    }
}
