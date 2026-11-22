// Healthcare integration basics: read a FHIR R4 Patient + Observation, validate, mask PHI for logs,
// and map an HL7 v2 ADT message. Built-in System.Text.Json only.
using System.Text.Json;
using System.Text.RegularExpressions;

const string patientJson = """
{
  "resourceType": "Patient", "id": "pat-001",
  "identifier": [{ "system": "http://hospital.example/mrn", "value": "MRN-445566" }],
  "name": [{ "family": "Rao", "given": ["Priya"] }],
  "gender": "female", "birthDate": "1984-07-19",
  "telecom": [{ "system": "phone", "value": "215-555-0142" }]
}
""";
const string observationJson = """
{
  "resourceType": "Observation", "id": "obs-9", "status": "final",
  "code": { "coding": [{ "system": "http://loinc.org", "code": "4548-4", "display": "Hemoglobin A1c" }] },
  "subject": { "reference": "Patient/pat-001" },
  "valueQuantity": { "value": 7.2, "unit": "%" }
}
""";

var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var patient = JsonSerializer.Deserialize<Patient>(patientJson, opts)!;
var obs = JsonSerializer.Deserialize<Observation>(observationJson, opts)!;

var errors = FhirValidator.Validate(patient);
Console.WriteLine($"FHIR Patient valid: {errors.Count == 0}");
Console.WriteLine($"Display (authorized UI): {patient.Name[0].Given[0]} {patient.Name[0].Family}, MRN {patient.Identifier[0].Value}");
Console.WriteLine($"Log line (PHI masked):  {PhiMasker.Mask(patient)}");
Console.WriteLine($"Lab: {obs.Code.Coding[0].Display} (LOINC {obs.Code.Coding[0].Code}) = {obs.ValueQuantity.Value}{obs.ValueQuantity.Unit} for {obs.Subject.Reference}");
Console.WriteLine($"Clinical flag: {(obs.ValueQuantity.Value >= 6.5m ? "above diabetes threshold, route to care team" : "normal")}");

const string adt = "MSH|^~\\&|ADT|HOSP|EHR|HOSP|202611220930||ADT^A01|MSG001|P|2.5\rPID|1||MRN-445566^^^HOSP^MR||Rao^Priya||19840719|F";
var hl7 = Hl7Parser.ParseAdt(adt);
Console.WriteLine($"HL7 v2 {hl7.Event}: MRN {hl7.Mrn} → FHIR Patient {hl7.ToFhirName()}");

// ---------- FHIR R4 models (minimal subset) ----------
public record Identifier(string System, string Value);
public record HumanName(string Family, string[] Given);
public record ContactPoint(string System, string Value);
public record Patient(string ResourceType, string Id, Identifier[] Identifier, HumanName[] Name, string Gender, string BirthDate, ContactPoint[] Telecom);
public record Coding(string System, string Code, string Display);
public record CodeableConcept(Coding[] Coding);
public record FhirReference(string Reference);
public record Quantity(decimal Value, string Unit);
public record Observation(string ResourceType, string Id, string Status, CodeableConcept Code, FhirReference Subject, Quantity ValueQuantity);

public static class FhirValidator
{
    public static List<string> Validate(Patient p)
    {
        var e = new List<string>();
        if (p.ResourceType != "Patient") e.Add("resourceType must be Patient");
        if (p.Identifier is not { Length: > 0 }) e.Add("at least one identifier (MRN) required");
        if (!DateOnly.TryParse(p.BirthDate, out _)) e.Add("birthDate must be YYYY-MM-DD");
        if (p.Gender is not ("male" or "female" or "other" or "unknown")) e.Add("gender must use the FHIR value set");
        return e;
    }
}

// HIPAA "minimum necessary": logs and analytics never carry direct identifiers
public static class PhiMasker
{
    public static string Mask(Patient p) =>
        $"patient={Hash(p.Identifier[0].Value)} name={p.Name[0].Family[0]}*** dob={p.BirthDate[..4]}-**-** phone={Regex.Replace(p.Telecom[0].Value, @"\d(?=(?:\D*\d){4})", "*")}";
    static string Hash(string s) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s)))[..12];
}

public record AdtMessage(string Event, string Mrn, string Family, string Given)
{
    public string ToFhirName() => $"{Given} {Family}";
}
public static class Hl7Parser
{
    public static AdtMessage ParseAdt(string message)
    {
        var segments = message.Split('\r').ToDictionary(s => s[..3], s => s.Split('|'));
        var msh = segments["MSH"]; var pid = segments["PID"];
        var name = pid[5].Split('^');
        return new AdtMessage(msh[8], pid[3].Split('^')[0], name[0], name[1]);
    }
}
