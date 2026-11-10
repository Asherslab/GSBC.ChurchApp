namespace GSBC.ChurchApp.Features.People.Api.Tests;

internal static class SmcSamples
{
    public static string Person(string id, string first, string surname, string email = "", string? dob = null, string middle = "",
        string wwccId = "", string? wwccExpiry = null, string wwccChecked = "-0001-11-30 00:00:00", string wwccStatus = "", string wwccNote = "",
        string status = "Approved", string statusWords = "", string? nextAction = "-0001-11-30", string progress = "", string police = "",
        string referee1 = "", string referee2 = "", string photo = "", string disclosure = "", string worksWithChildren = "0") =>
        $$"""
        {
            "smc_id": "{{id}}", "firstname": "{{first}}", "middlename": "{{middle}}", "surname": "{{surname}}",
            "email": "{{email}}", "mobile": "0400000000", "dob": {{Json(dob)}},
            "status": "{{status}}", "status_description": "{{statusWords}}", "next_action_date": {{Json(nextAction)}},
            "type": "Leader", "course_progress": "{{progress}}", "works_with_children": "{{worksWithChildren}}",
            "wwcc_id": "{{wwccId}}", "wwcc_expiry": {{Json(wwccExpiry)}}, "wwcc_photo_status": "{{photo}}",
            "paperwork_process_status": "0", "wwcc_status": "{{wwccStatus}}", "wwcc_checked": "{{wwccChecked}}",
            "wwcc_status_description": "{{wwccNote}}", "police_check_status": "{{police}}",
            "previous_experience": "", "trainee_role": "", "date_signed_COC": null, "trainee_disclosure": "{{disclosure}}",
            "referee1_response": "{{referee1}}", "referee2_response": "{{referee2}}", "admin_notes": "Private notes about them",
            "other_training_name": null, "other_training_completed_date": null, "other_sys_id": ""
        }
        """;

    public static string Page(int page, int pages, params string[] people) =>
        $$"""{ "people": [{{string.Join(",", people)}}], "pagination": { "total": {{people.Length}}, "page": {{page}}, "pageSize": 1000, "pages": {{pages}} } }""";

    private static string Json(string? value) => value is null ? "null" : $"\"{value}\"";
}
