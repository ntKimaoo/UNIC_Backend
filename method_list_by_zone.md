# Danh Sách Method Test — Phân Theo Zone

> Dữ liệu lấy trực tiếp từ source code. Chỉ bao gồm **Service** và **Controller** layer.

---

## 🟦 ZONE 1 — Auth · User · JWT · Email · FileStorage

| # | File | Tầng | Method |
|---|------|------|--------|
| 1 | AuthServiceTest | Service | Login_ShouldReturnTokenPair_WhenCredentialsValid |
| 2 | AuthServiceTest | Service | Login_ShouldThrowNotFoundException_WhenUserNotFound |
| 3 | AuthServiceTest | Service | Login_ShouldThrowDomainException_WhenPasswordIsWrong |
| 4 | AuthServiceTest | Service | Login_ShouldThrowDomainException_WhenEmailNotVerified |
| 5 | AuthServiceTest | Service | Register_ShouldThrowDomainException_WhenEmailExists |
| 6 | AuthServiceTest | Service | Register_ShouldThrowDomainException_WhenStudentIdExists |
| 7 | AuthServiceTest | Service | Register_ShouldReturnTokens_WhenValid |
| 8 | AuthServiceTest | Service | VerifyEmail_ShouldThrowNotFoundException_WhenTokenNotFound |
| 9 | AuthServiceTest | Service | VerifyEmail_ShouldThrowDomainException_WhenTokenExpiredOrUsed |
| 10 | AuthServiceTest | Service | VerifyEmail_ShouldActivateUserAndInvalidateTokens_WhenValid |
| 11 | AuthServiceTest | Service | ForgotPassword_ShouldThrowNotFoundException_WhenEmailNotFound |
| 12 | AuthServiceTest | Service | ForgotPassword_ShouldSendEmail_WhenValid |
| 13 | AuthServiceTest | Service | ResetPassword_ShouldThrowNotFoundException_WhenTokenNotFound |
| 14 | AuthServiceTest | Service | ResetPassword_ShouldThrowDomainException_WhenTokenExpiredOrUsed |
| 15 | AuthServiceTest | Service | ResetPassword_ShouldUpdatePasswordAndInvalidate_WhenValid |
| 16 | AuthServiceTest | Service | RefreshToken_ShouldThrowDomainException_WhenTokenNotFound |
| 17 | AuthServiceTest | Service | RefreshToken_ShouldThrowDomainException_WhenTokenRevoked |
| 18 | AuthServiceTest | Service | RefreshToken_ShouldThrowDomainException_WhenTokenExpired |
| 19 | AuthServiceTest | Service | RefreshToken_ShouldReturnNewTokenPair_WhenValid |
| 20 | AuthServiceTest | Service | Logout_ShouldRevokeAllTokens_WhenCalled |
| 21 | AuthServiceTest | Service | ChangePassword_ShouldThrowNotFoundException_WhenUserNotFound |
| 22 | AuthServiceTest | Service | ChangePassword_ShouldThrowDomainException_WhenOldPasswordWrong |
| 23 | AuthServiceTest | Service | ChangePassword_ShouldUpdatePassword_WhenValid |
| 24 | AuthServiceTest | Service | ResendVerificationEmail_ShouldThrowNotFoundException_WhenUserNotFound |
| 25 | UserServiceTest | Service | GetAllUsersAsync_ShouldReturnAllUsers |
| 26 | UserServiceTest | Service | GetUserByIdAsync_ShouldReturnNull_WhenUserNotFound |
| 27 | UserServiceTest | Service | GetUserByIdAsync_ShouldReturnUser_WhenExists |
| 28 | UserServiceTest | Service | CreateUserAsync_ShouldThrowException_WhenEmailExists |
| 29 | UserServiceTest | Service | CreateUserAsync_ShouldThrowException_WhenStudentIdExists |
| 30 | UserServiceTest | Service | CreateUserAsync_ShouldReturnUser_WhenSuccessful |
| 31 | UserServiceTest | Service | UpdateUserAsync_ShouldReturnFalse_WhenUserNotFound |
| 32 | UserServiceTest | Service | UpdateUserAsync_ShouldThrowException_WhenStudentIdExistsForOtherUser |
| 33 | UserServiceTest | Service | UpdateUserAsync_ShouldReturnTrue_WhenSuccessful |
| 34 | UserServiceTest | Service | DeleteUserAsync_ShouldReturnRepositoryResult |
| 35 | JwtServiceTest | Service | GenerateAccessToken_ShouldReturnValidJwtToken |
| 36 | JwtServiceTest | Service | GenerateRefreshToken_ShouldReturnBase64String |
| 37 | JwtServiceTest | Service | ValidateAccessToken_ShouldReturnUserId_WhenTokenContainsIntSub |
| 38 | JwtServiceTest | Service | ValidateAccessToken_ShouldReturnNull_WhenSubIndInvalidFormat |
| 39 | JwtServiceTest | Service | ValidateAccessToken_ShouldReturnNull_WhenTokenInvalid |
| 40 | EmailServiceTest | Service | SendVerificationEmailAsync_ShouldReturnFalse_WhenSmtpFails |
| 41 | EmailServiceTest | Service | SendPasswordResetEmailAsync_ShouldReturnFalse_WhenSmtpFails |
| 42 | EmailServiceTest | Service | SendWelcomeEmailAsync_ShouldReturnFalse_WhenSmtpFails |
| 43 | FileStorageServiceTest | Service | ValidateImageFile_ShouldReturnFalse_WhenFileIsNull |
| 44 | FileStorageServiceTest | Service | ValidateImageFile_ShouldReturnFalse_WhenFileIsEmpty |
| 45 | FileStorageServiceTest | Service | ValidateImageFile_ShouldReturnFalse_WhenFileIsTooLarge |
| 46 | FileStorageServiceTest | Service | ValidateImageFile_ShouldReturnFalse_WhenExtensionIsInvalid |
| 47 | FileStorageServiceTest | Service | ValidateImageFile_ShouldReturnTrue_WhenFileIsValid |
| 48 | FileStorageServiceTest | Service | SaveFileAsync_ShouldThrowException_WhenValidationFails |
| 49 | FileStorageServiceTest | Service | DeleteFileAsync_ShouldReturnFalse_WhenUrlIsNullOrEmpty |
| 50 | FileStorageServiceTest | Service | DeleteFileAsync_ShouldReturnFalse_OnException |
| 51 | AuthControllerTest | Controller | Login_ReturnsOk_WhenValid |
| 52 | AuthControllerTest | Controller | Login_ReturnsBadRequest_WhenInvalid |
| 53 | AuthControllerTest | Controller | Login_ReturnsUnauthorized_WhenCredentialsFail |
| 54 | AuthControllerTest | Controller | Register_ReturnsCreated_WhenSuccess |
| 55 | AuthControllerTest | Controller | Register_ReturnsBadRequest_WhenInvalid |
| 56 | AuthControllerTest | Controller | Register_ReturnsBadRequest_WhenEmailExists |
| 57 | AuthControllerTest | Controller | VerifyEmail_ReturnsOk_WhenValid |
| 58 | AuthControllerTest | Controller | VerifyEmail_ReturnsBadRequest_WhenTokenInvalid |
| 59 | AuthControllerTest | Controller | RefreshToken_ReturnsOk_WhenValid |
| 60 | AuthControllerTest | Controller | RefreshToken_ReturnsBadRequest_WhenInvalid |
| 61 | AuthControllerTest | Controller | ForgotPassword_ReturnsOk_WhenValid |
| 62 | AuthControllerTest | Controller | ForgotPassword_ReturnsNotFound_WhenUserMissing |
| 63 | AuthControllerTest | Controller | ResetPassword_ReturnsOk_WhenValid |
| 64 | AuthControllerTest | Controller | ResetPassword_ReturnsBadRequest_WhenTokenExpired |
| 65 | AuthControllerTest | Controller | Logout_ReturnsOk_WhenValid |
| 66 | AuthControllerTest | Controller | Logout_ReturnsBadRequest_WhenTokenInvalid |
| 67 | AuthControllerTest | Controller | ChangePassword_ReturnsOk_WhenValid |
| 68 | AuthControllerTest | Controller | ChangePassword_ReturnsBadRequest_WhenWrongPassword |
| 69 | AuthControllerTest | Controller | ResendVerification_ReturnsOk_WhenValid |
| 70 | AuthControllerTest | Controller | ResendVerification_ReturnsNotFound_WhenUserMissing |
| 71 | AuthControllerTest | Controller | GoogleSignIn_ReturnsOk_WhenValid |
| 72 | AuthControllerTest | Controller | GoogleSignIn_ReturnsBadRequest_WhenTokenInvalid |
| 73 | AuthControllerTest | Controller | GetProfile_ReturnsOk_WhenAuthenticated |
| 74 | AuthControllerTest | Controller | GetProfile_ReturnsUnauthorized_WhenNotAuthenticated |
| 75 | UsersControllerTest | Controller | GetAll_ReturnsOk_WhenFound |
| 76 | UsersControllerTest | Controller | GetAll_ReturnsNotFound_WhenEmpty |
| 77 | UsersControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 78 | UsersControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 79 | UsersControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 80 | UsersControllerTest | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 81 | UsersControllerTest | Controller | Update_ReturnsOk_WhenFound |
| 82 | UsersControllerTest | Controller | Update_ReturnsNotFound_WhenMissing |
| 83 | UsersControllerTest | Controller | Update_ReturnsBadRequest_WhenServiceThrows |
| 84 | UsersControllerTest | Controller | Delete_ReturnsOk_WhenFound |
| 85 | UsersControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |

**Tổng Zone 1: 85 methods**

---

## 🟩 ZONE 2 — Club · Department · ClubRole · Policy

| # | File | Tầng | Method |
|---|------|------|--------|
| 1 | ClubServiceTest | Service | GetByIdAsync_ShouldReturnNull_WhenNotFound |
| 2 | ClubServiceTest | Service | GetByIdAsync_ShouldReturnDto_WhenExists |
| 3 | ClubServiceTest | Service | GetAllAsync_ShouldReturnAllClubs |
| 4 | ClubServiceTest | Service | GetActiveClubsAsync_ShouldReturnActiveClubs |
| 5 | ClubServiceTest | Service | GetPublicClubsAsync_ShouldReturnPublicClubs |
| 6 | ClubServiceTest | Service | CreateAsync_ShouldThrowException_WhenNameExists |
| 7 | ClubServiceTest | Service | CreateAsync_ShouldReturnDto_WhenCreated |
| 8 | ClubServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenClubNotFound |
| 9 | ClubServiceTest | Service | UpdateAsync_ShouldThrowException_WhenNewNameExistsForOtherClub |
| 10 | ClubServiceTest | Service | UpdateAsync_ShouldAllowSameNameUpdate_WhenNameIsItsOwn |
| 11 | ClubServiceTest | Service | UpdateAsync_ShouldUpdateFieldsAndReturnDto_WhenValid |
| 12 | ClubServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo |
| 13 | ClubServiceTest | Service | DeleteAsync_ShouldReturnRepositoryResult |
| 14 | ClubServiceTest | Service | SoftDeleteAsync_ShouldReturnRepositoryResult |
| 15 | ClubServiceTest | Service | ChangeStatusClub_ShouldCallRepository |
| 16 | DepartmentServiceTest | Service | CreateDepartmentAsync_ShouldReturnMappedDto |
| 17 | DepartmentServiceTest | Service | DeleteDepartmentAsync_ShouldReturnRepoResult |
| 18 | DepartmentServiceTest | Service | GetAllDepartmentsAsync_ShouldReturnMappedDtos |
| 19 | DepartmentServiceTest | Service | GetDepartmentByIdAsync_ShouldReturnNull_WhenNotFound |
| 20 | DepartmentServiceTest | Service | GetDepartmentByIdAsync_ShouldReturnMappedDto_WhenFound |
| 21 | DepartmentServiceTest | Service | UpdateDepartmentAsync_ShouldReturnFalse_WhenNotFound |
| 22 | DepartmentServiceTest | Service | UpdateDepartmentAsync_ShouldReturnFalse_WhenIdsDontMatch |
| 23 | DepartmentServiceTest | Service | UpdateDepartmentAsync_ShouldUpdateAndReturnTrue_WhenValid |
| 24 | ClubRoleServiceTest | Service | GetByIdAsync_ShouldReturnNull_WhenRoleNotFound |
| 25 | ClubRoleServiceTest | Service | GetByIdAsync_ShouldReturnMappedDto_WhenFound |
| 26 | ClubRoleServiceTest | Service | GetAllAsync_ShouldReturnMappedDtos |
| 27 | ClubRoleServiceTest | Service | GetPoliciesByRoleAsync_ShouldReturnPolicies |
| 28 | ClubRoleServiceTest | Service | CreateAsync_ShouldThrowException_WhenRoleNameExists |
| 29 | ClubRoleServiceTest | Service | CreateAsync_ShouldCreateRoleAndPolicies_WhenValid |
| 30 | ClubRoleServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenRoleNotFound |
| 31 | ClubRoleServiceTest | Service | UpdateAsync_ShouldThrowException_WhenNewRoleNameExists |
| 32 | ClubRoleServiceTest | Service | UpdateAsync_ShouldUpdateRoleAndPolicies_WhenValid |
| 33 | ClubRoleServiceTest | Service | UpdatePoliciesAsync_ShouldCallRepository |
| 34 | ClubRoleServiceTest | Service | DeleteAsync_ShouldReturnRepoResult |
| 35 | PolicyServiceTest | Service | GetUserPoliciesAsync_ShouldReturnPolicies |
| 36 | PolicyServiceTest | Service | HasUserPolicyAsync_ShouldReturnRepoResult |
| 37 | PolicyServiceTest | Service | GetAllPolicyGroupAsync_ShouldReturnGroups |
| 38 | PolicyServiceTest | Service | GetAllPoliciesByGroupAsync_ShouldReturnPolicies |
| 39 | ClubControllerTest | Controller | GetAll_ReturnsOk |
| 40 | ClubControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 41 | ClubControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 42 | ClubControllerTest | Controller | GetActive_ReturnsOk |
| 43 | ClubControllerTest | Controller | GetPublic_ReturnsOk |
| 44 | ClubControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 45 | ClubControllerTest | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 46 | ClubControllerTest | Controller | Update_ReturnsOk_WhenSuccess |
| 47 | ClubControllerTest | Controller | Update_ReturnsNotFound_WhenNull |
| 48 | ClubControllerTest | Controller | Update_ReturnsBadRequest_WhenServiceThrows |
| 49 | ClubControllerTest | Controller | Delete_ReturnsOk_WhenFound |
| 50 | ClubControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |
| 51 | ClubControllerTest | Controller | ChangeStatus_ReturnsOk_WhenSuccess |
| 52 | ClubControllerTest | Controller | ChangeStatus_ReturnsNotFound_WhenNull |
| 53 | ClubControllerTest | Controller | GetMembers_ReturnsOk_WhenFound |
| 54 | ClubControllerTest | Controller | GetMembers_ReturnsNotFound_WhenClubMissing |
| 55 | ClubControllerTest | Controller | Upload_ReturnsOk_WhenSuccess |
| 56 | DepartmentControllerTest | Controller | GetAll_ReturnsOk_WhenFound |
| 57 | DepartmentControllerTest | Controller | GetAll_ReturnsNotFound_WhenEmpty |
| 58 | DepartmentControllerTest | Controller | GetDepartmentById_ReturnsOk_WhenFound |
| 59 | DepartmentControllerTest | Controller | GetDepartmentById_ReturnsNotFound_WhenNull |
| 60 | DepartmentControllerTest | Controller | UpdateDepartment_ReturnsOk_WhenFound |
| 61 | DepartmentControllerTest | Controller | UpdateDepartment_ReturnsNotFound_WhenMissing |
| 62 | DepartmentControllerTest | Controller | CreateDepartment_ReturnsCreated |
| 63 | DepartmentControllerTest | Controller | DeleteDepartment_ReturnsOk_WhenFound |
| 64 | DepartmentControllerTest | Controller | DeleteDepartment_ReturnsNotFound_WhenMissing |
| 65 | ClubRoleControllerTest | Controller | GetAll_ReturnsOk |
| 66 | ClubRoleControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 67 | ClubRoleControllerTest | Controller | GetById_ReturnsNotFound_WhenEmpty |
| 68 | ClubRoleControllerTest | Controller | GetPoliciesById_ReturnsOk_WhenFound |
| 69 | ClubRoleControllerTest | Controller | GetPoliciesById_ReturnsNotFound_WhenNull |
| 70 | ClubRoleControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 71 | ClubRoleControllerTest | Controller | Create_ReturnsBadRequest_WhenInvalidOperation |
| 72 | ClubRoleControllerTest | Controller | Create_Returns500_WhenUnexpected |
| 73 | ClubRoleControllerTest | Controller | Update_ReturnsOk_WhenSuccess |
| 74 | ClubRoleControllerTest | Controller | Update_ReturnsNotFound_WhenNull |
| 75 | ClubRoleControllerTest | Controller | UpdatePolicies_ReturnsOk_WhenSuccess |
| 76 | ClubRoleControllerTest | Controller | UpdatePolicies_ReturnsBadRequest_WhenInvalidOperation |
| 77 | ClubRoleControllerTest | Controller | Delete_ReturnsOk_WhenFound |
| 78 | ClubRoleControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |
| 79 | PolicyControllerTest | Controller | GetAll_ReturnsOk |
| 80 | PolicyControllerTest | Controller | GetAllGroupById_ReturnsOk |

**Tổng Zone 2: 80 methods**

---

## 🟨 ZONE 3 — ClubMember · RecruitmentCampaign · Application (phần 1)

| # | File | Tầng | Method |
|---|------|------|--------|
| 1 | ClubMemberServiceTest | Service | GetMembersByClubId_ShouldReturnMembers |
| 2 | ClubMemberServiceTest | Service | GetById_ShouldReturnMember_WhenExists |
| 3 | ClubMemberServiceTest | Service | GetById_ShouldThrow_WhenNotFound |
| 4 | ClubMemberServiceTest | Service | AddMember_ShouldAdd_WhenValid |
| 5 | ClubMemberServiceTest | Service | AddMember_ShouldThrow_WhenAlreadyMember |
| 6 | ClubMemberServiceTest | Service | IsMember_ShouldReturnTrue_WhenMemberExists |
| 7 | ClubMemberServiceTest | Service | IsMember_ShouldReturnFalse_WhenNotMember |
| 8 | ClubMemberServiceTest | Service | RemoveMember_ShouldRemove_WhenExists |
| 9 | ClubMemberServiceTest | Service | RemoveMember_ShouldThrow_WhenNotFound |
| 10 | ClubMemberServiceTest | Service | UpdateMemberRole_ShouldUpdate_WhenValid |
| 11 | ClubMemberServiceTest | Service | UpdateMemberRole_ShouldThrow_WhenMemberNotFound |
| 12 | RecruitmentCampaignServiceTest | Service | GetByIdAsync_ShouldReturnNull_WhenNotFound |
| 13 | RecruitmentCampaignServiceTest | Service | GetByIdAsync_ShouldReturnMappedDto_WhenFound |
| 14 | RecruitmentCampaignServiceTest | Service | GetAllAsync_ShouldReturnMappedDtos |
| 15 | RecruitmentCampaignServiceTest | Service | GetByClubIdAsync_ShouldReturnMappedDtos |
| 16 | RecruitmentCampaignServiceTest | Service | CreateAsync_ShouldReturnMappedDto |
| 17 | RecruitmentCampaignServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenNotFound |
| 18 | RecruitmentCampaignServiceTest | Service | UpdateAsync_ShouldUpdateFieldsAndReturnDto_WhenValid |
| 19 | RecruitmentCampaignServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo |
| 20 | RecruitmentCampaignServiceTest | Service | DeleteAsync_ShouldReturnRepoResult |
| 21 | ApplicationServiceTest | Service | GetAll_ShouldReturnApplications |
| 22 | ApplicationServiceTest | Service | GetById_ShouldReturn_WhenExists |
| 23 | ApplicationServiceTest | Service | GetById_ShouldThrow_WhenNotFound |
| 24 | ApplicationServiceTest | Service | GetByUserId_ShouldReturnUserApplications |
| 25 | ApplicationServiceTest | Service | Create_ShouldAddApplication_WhenValid |
| 26 | ApplicationServiceTest | Service | Create_ShouldThrow_WhenDuplicate |
| 27 | ApplicationServiceTest | Service | UpdateStatus_ShouldModify_WhenValid |
| 28 | ApplicationServiceTest | Service | UpdateStatus_ShouldThrow_WhenNotFound |
| 29 | ApplicationServiceTest | Service | GetFormsByCampaignId_ShouldReturnForms |
| 30 | ApplicationServiceTest | Service | CreateForm_ShouldAddForm |
| 31 | ApplicationServiceTest | Service | CreateQuestion_ShouldAddQuestion |
| 32 | ApplicationServiceTest | Service | GetQuestionsByCampaignId_ShouldReturnQuestions |
| 33 | ClubMemberControllerTest | Controller | GetByClubId_ReturnsOk |
| 34 | ClubMemberControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 35 | ClubMemberControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 36 | ClubMemberControllerTest | Controller | Add_ReturnsCreated_WhenSuccess |
| 37 | ClubMemberControllerTest | Controller | Add_ReturnsBadRequest_WhenServiceThrows |
| 38 | ClubMemberControllerTest | Controller | IsMember_ReturnsOk_WhenFound |
| 39 | ClubMemberControllerTest | Controller | IsMember_ReturnsNotFound_WhenNull |
| 40 | ClubMemberControllerTest | Controller | Remove_ReturnsOk_WhenSuccess |
| 41 | ClubMemberControllerTest | Controller | Remove_ReturnsNotFound_WhenMissing |
| 42 | ClubMemberControllerTest | Controller | Remove_ReturnsBadRequest_WhenServiceThrows |
| 43 | ClubMemberControllerTest | Controller | UpdateRole_ReturnsOk_WhenSuccess |
| 44 | ClubMemberControllerTest | Controller | UpdateRole_ReturnsNotFound_WhenMissing |
| 45 | ClubMemberControllerTest | Controller | UpdateRole_ReturnsBadRequest_WhenServiceThrows |
| 46 | ClubMemberControllerTest | Controller | GetAll_ReturnsOk |
| 47 | RecruitmentCampaignControllerTest | Controller | GetAll_ReturnsOk |
| 48 | RecruitmentCampaignControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 49 | RecruitmentCampaignControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 50 | RecruitmentCampaignControllerTest | Controller | GetByClubId_ReturnsOk |
| 51 | RecruitmentCampaignControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 52 | RecruitmentCampaignControllerTest | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 53 | RecruitmentCampaignControllerTest | Controller | Update_ReturnsOk_WhenSuccess |
| 54 | RecruitmentCampaignControllerTest | Controller | Update_ReturnsNotFound_WhenNull |
| 55 | RecruitmentCampaignControllerTest | Controller | Update_ReturnsBadRequest_WhenServiceThrows |
| 56 | RecruitmentCampaignControllerTest | Controller | Delete_ReturnsOk_WhenFound |
| 57 | RecruitmentCampaignControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |
| 58 | ApplicationControllerTest *(P1)* | Controller | GetAll_ReturnsOk |
| 59 | ApplicationControllerTest *(P1)* | Controller | GetById_ReturnsOk_WhenFound |
| 60 | ApplicationControllerTest *(P1)* | Controller | GetById_ReturnsNotFound_WhenNull |
| 61 | ApplicationControllerTest *(P1)* | Controller | GetByUserId_ReturnsOk |
| 62 | ApplicationControllerTest *(P1)* | Controller | GetByUserId_ReturnsNotFound_WhenEmpty |
| 63 | ApplicationControllerTest *(P1)* | Controller | GetByCampaignId_ReturnsOk |
| 64 | ApplicationControllerTest *(P1)* | Controller | GetByCampaignId_ReturnsNotFound_WhenEmpty |
| 65 | ApplicationControllerTest *(P1)* | Controller | Create_ReturnsCreated_WhenSuccess |
| 66 | ApplicationControllerTest *(P1)* | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 67 | ApplicationControllerTest *(P1)* | Controller | UpdateStatus_ReturnsOk_WhenSuccess |
| 68 | ApplicationControllerTest *(P1)* | Controller | UpdateStatus_ReturnsNotFound_WhenNull |
| 69 | ApplicationControllerTest *(P1)* | Controller | UpdateStatus_ReturnsBadRequest_WhenServiceThrows |
| 70 | ApplicationControllerTest *(P1)* | Controller | GetFormsByCampaignId_ReturnsOk |
| 71 | ApplicationControllerTest *(P1)* | Controller | GetFormsByCampaignId_ReturnsNotFound_WhenEmpty |
| 72 | ApplicationControllerTest *(P1)* | Controller | CreateForm_ReturnsCreated_WhenSuccess |
| 73 | ApplicationControllerTest *(P1)* | Controller | CreateForm_ReturnsBadRequest_WhenServiceThrows |
| 74 | ApplicationControllerTest *(P1)* | Controller | GetQuestionsByCampaign_ReturnsOk |
| 75 | ApplicationControllerTest *(P1)* | Controller | GetQuestionsByCampaign_ReturnsNotFound_WhenEmpty |
| 76 | ApplicationControllerTest *(P1)* | Controller | CreateQuestion_ReturnsCreated_WhenSuccess |
| 77 | ApplicationControllerTest *(P1)* | Controller | CreateQuestion_ReturnsBadRequest_WhenServiceThrows |
| 78 | ApplicationControllerTest *(P1)* | Controller | GetUserAnswerByForm_ReturnsOk_WhenFound |
| 79 | ApplicationControllerTest *(P1)* | Controller | GetUserAnswerByForm_ReturnsNotFound_WhenNull |
| 80 | ApplicationControllerTest *(P1)* | Controller | SubmitAnswers_ReturnsOk_WhenSuccess |
| 81 | ApplicationControllerTest *(P1)* | Controller | SubmitAnswers_ReturnsBadRequest_WhenServiceThrows |
| 82 | ApplicationControllerTest *(P1)* | Controller | GetAllByForm_ReturnsOk_WhenFound |
| 83 | ApplicationControllerTest *(P1)* | Controller | GetAllByForm_ReturnsNotFound_WhenNull |

**Tổng Zone 3: 83 methods**

---

## 🟧 ZONE 4 — Event · Attendance · Application (phần 2)

| # | File | Tầng | Method |
|---|------|------|--------|
| 1 | EventServiceTest | Service | CreateEventAsync_ShouldThrowDomainException_WhenValidationFails |
| 2 | EventServiceTest | Service | CreateEventAsync_ShouldCreateAndSaveEvent_WhenValid |
| 3 | EventServiceTest | Service | UpdateEventAsync_ShouldThrowDomainException_WhenValidationFails |
| 4 | EventServiceTest | Service | UpdateEventAsync_ShouldThrowNotFoundException_WhenEventNotFound |
| 5 | EventServiceTest | Service | UpdateEventAsync_ShouldThrowDomainException_WhenEventStatusInvalid |
| 6 | EventServiceTest | Service | UpdateEventAsync_ShouldUpdate_WhenValid |
| 7 | EventServiceTest | Service | CreateSessionAsync_ShouldThrow_WhenValidationFails |
| 8 | EventServiceTest | Service | CreateSessionAsync_ShouldThrowNotFound_WhenEventNotFound |
| 9 | EventServiceTest | Service | CreateSessionAsync_ShouldThrow_WhenTimeInvalid |
| 10 | EventServiceTest | Service | CreateSessionAsync_ShouldAddAndSave_WhenValid |
| 11 | EventServiceTest | Service | OpenRegistrationAsync_ShouldThrow_WhenEventStatusNotPlanned |
| 12 | EventServiceTest | Service | OpenRegistrationAsync_ShouldThrow_WhenEndDateAfterEventStart |
| 13 | EventServiceTest | Service | OpenRegistrationAsync_ShouldUpdateStatusAndSave_WhenValid |
| 14 | EventServiceTest | Service | GetEventByIdAsync_ShouldThrowNotFound |
| 15 | EventServiceTest | Service | GetEventByIdAsync_ShouldReturnDto |
| 16 | EventServiceTest | Service | GetAllEventsAsync_ShouldReturnDtos |
| 17 | AttendanceServiceTest | Service | GetByEventAndUser_ShouldReturn_WhenExists |
| 18 | AttendanceServiceTest | Service | GetByEventAndUser_ShouldThrow_WhenNotFound |
| 19 | AttendanceServiceTest | Service | IsUserRegistered_ShouldReturnTrue_WhenRegistered |
| 20 | AttendanceServiceTest | Service | IsUserRegistered_ShouldReturnFalse_WhenNotRegistered |
| 21 | AttendanceServiceTest | Service | GetAllByEvent_ShouldReturnAttendees |
| 22 | AttendanceServiceTest | Service | RegisterAttendance_ShouldAdd_WhenValid |
| 23 | AttendanceServiceTest | Service | RegisterAttendance_ShouldThrow_WhenAlreadyExists |
| 24 | AttendanceServiceTest | Service | UnregisterAttendance_ShouldRemove_WhenValid |
| 25 | AttendanceServiceTest | Service | UnregisterAttendance_ShouldThrow_WhenNotFound |
| 26 | AttendanceServiceTest | Service | CheckIn_ShouldUpdate_WhenValid |
| 27 | AttendanceServiceTest | Service | CheckIn_ShouldThrow_WhenNotFound |
| 28 | AttendanceServiceTest | Service | CheckIn_ShouldThrow_WhenAlreadyCheckedIn |
| 29 | AttendanceServiceTest | Service | GenerateQrCode_ShouldReturnQrData |
| 30 | AttendanceServiceTest | Service | ValidateQrCode_ShouldReturnAttendance_WhenValid |
| 31 | AttendanceServiceTest | Service | ValidateQrCode_ShouldThrow_WhenExpired |
| 32 | AttendanceServiceTest | Service | GetMyAttendance_ShouldReturnUserAttendances |
| 33 | AttendanceServiceTest | Service | GetAbsent_ShouldReturnAbsentees |
| 34 | AttendanceServiceTest | Service | GetPresent_ShouldReturnPresentees |
| 35 | AttendanceServiceTest | Service | ExportAttendance_ShouldReturnExcelData |
| 36 | EventsControllerTest | Controller | GetAllEvents_ReturnsOk_WithValidPagination |
| 37 | EventsControllerTest | Controller | GetAllEvents_ReturnsBadRequest_WhenInvalidPageNumber |
| 38 | EventsControllerTest | Controller | GetAllEvents_ReturnsBadRequest_WhenPageSizeTooLarge |
| 39 | EventsControllerTest | Controller | GetEventById_ReturnsOk_WhenFound |
| 40 | EventsControllerTest | Controller | GetEventById_ReturnsNotFound_WhenNotFound |
| 41 | EventsControllerTest | Controller | GetEventById_Returns500_WhenUnexpected |
| 42 | EventsControllerTest | Controller | CreateEvent_ReturnsCreated_WithNoImage |
| 43 | EventsControllerTest | Controller | CreateEvent_ReturnsCreated_WithImage |
| 44 | EventsControllerTest | Controller | CreateEvent_ReturnsBadRequest_WhenInvalidOperation |
| 45 | EventsControllerTest | Controller | CreateEvent_Returns500_WhenUnexpected |
| 46 | EventsControllerTest | Controller | UpdateEvent_ReturnsOk_WhenSuccess |
| 47 | EventsControllerTest | Controller | UpdateEvent_ReturnsBadRequest_WhenIdMismatch |
| 48 | EventsControllerTest | Controller | UpdateEvent_ReturnsNotFound_WhenNotFound |
| 49 | EventsControllerTest | Controller | CreateSession_ReturnsCreated_WhenSuccess |
| 50 | EventsControllerTest | Controller | CreateSession_ReturnsBadRequest_WhenIdMismatch |
| 51 | EventsControllerTest | Controller | CreateSession_ReturnsNotFound_WhenEventNotFound |
| 52 | EventsControllerTest | Controller | OpenRegistration_ReturnsOk_WhenSuccess |
| 53 | EventsControllerTest | Controller | OpenRegistration_ReturnsBadRequest_WhenIdMismatch |
| 54 | AttendanceControllerTest | Controller | GetByEventAndUser_ReturnsOk_WhenFound |
| 55 | AttendanceControllerTest | Controller | GetByEventAndUser_ReturnsNotFound_WhenNull |
| 56 | AttendanceControllerTest | Controller | IsRegistered_ReturnsOk_WhenTrue |
| 57 | AttendanceControllerTest | Controller | Register_ReturnsCreated_WhenSuccess |
| 58 | AttendanceControllerTest | Controller | Register_ReturnsBadRequest_WhenServiceThrows |
| 59 | AttendanceControllerTest | Controller | Unregister_ReturnsOk_WhenSuccess |
| 60 | AttendanceControllerTest | Controller | Unregister_ReturnsNotFound_WhenMissing |
| 61 | AttendanceControllerTest | Controller | CheckIn_ReturnsOk_WhenSuccess |
| 62 | AttendanceControllerTest | Controller | CheckIn_ReturnsBadRequest_WhenServiceThrows |
| 63 | AttendanceControllerTest | Controller | GenerateQr_ReturnsOk_WhenSuccess |
| 64 | AttendanceControllerTest | Controller | ValidateQr_ReturnsOk_WhenValid |
| 65 | AttendanceControllerTest | Controller | ValidateQr_ReturnsBadRequest_WhenExpired |
| 66 | AttendanceControllerTest | Controller | GetAbsent_ReturnsOk |
| 67 | AttendanceControllerTest | Controller | GetPresent_ReturnsOk |
| 68 | ApplicationControllerTest *(P2)* | Controller | GetPendingByForm_ReturnsOk_WhenFound |
| 69 | ApplicationControllerTest *(P2)* | Controller | GetPendingByForm_ReturnsNotFound_WhenNull |
| 70 | ApplicationControllerTest *(P2)* | Controller | GetApprovedByForm_ReturnsOk_WhenFound |
| 71 | ApplicationControllerTest *(P2)* | Controller | GetApprovedByForm_ReturnsNotFound_WhenNull |
| 72 | ApplicationControllerTest *(P2)* | Controller | GetRejectedByForm_ReturnsOk_WhenFound |
| 73 | ApplicationControllerTest *(P2)* | Controller | GetRejectedByForm_ReturnsNotFound_WhenNull |
| 74 | ApplicationControllerTest *(P2)* | Controller | BatchApprove_ReturnsOk_WhenSuccess |
| 75 | ApplicationControllerTest *(P2)* | Controller | BatchApprove_ReturnsBadRequest_WhenServiceThrows |
| 76 | ApplicationControllerTest *(P2)* | Controller | BatchReject_ReturnsOk_WhenSuccess |
| 77 | ApplicationControllerTest *(P2)* | Controller | BatchReject_ReturnsBadRequest_WhenServiceThrows |
| 78 | ApplicationControllerTest *(P2)* | Controller | GetInterviewSchedules_ReturnsOk |
| 79 | ApplicationControllerTest *(P2)* | Controller | GetInterviewSchedules_ReturnsNotFound_WhenNull |
| 80 | ApplicationControllerTest *(P2)* | Controller | CreateInterviewSchedule_ReturnsCreated_WhenSuccess |
| 81 | ApplicationControllerTest *(P2)* | Controller | CreateInterviewSchedule_ReturnsBadRequest_WhenServiceThrows |

**Tổng Zone 4: 81 methods**

---

## 🟥 ZONE 5 — ClubFund · ClubPost · Interview · Rooms

| # | File | Tầng | Method |
|---|------|------|--------|
| 1 | ClubFundServiceTest | Service | GetFundByClubId_ShouldReturn_WhenExists |
| 2 | ClubFundServiceTest | Service | GetFundByClubId_ShouldThrow_WhenNotFound |
| 3 | ClubFundServiceTest | Service | GetTransactions_ShouldReturnTransactions |
| 4 | ClubFundServiceTest | Service | AddTransaction_ShouldAdd_WhenValid |
| 5 | ClubFundServiceTest | Service | AddTransaction_ShouldThrow_WhenFundNotFound |
| 6 | ClubFundServiceTest | Service | GetBalance_ShouldReturnCurrentBalance |
| 7 | ClubFundServiceTest | Service | UpdateFund_ShouldModify_WhenValid |
| 8 | ClubFundServiceTest | Service | UpdateFund_ShouldThrow_WhenNotFound |
| 9 | ClubFundServiceTest | Service | CreateFund_ShouldCreate_WhenValid |
| 10 | ClubFundServiceTest | Service | GetTransactionById_ShouldReturn_WhenExists |
| 11 | ClubPostServiceTest | Service | CreateAsync_ShouldReturnNull_WhenCreationFails |
| 12 | ClubPostServiceTest | Service | CreateAsync_ShouldCreatePost_WhenNoImage |
| 13 | ClubPostServiceTest | Service | CreateAsync_ShouldUpdateStatusToPendingAndEnqueue_WhenImageProvided |
| 14 | ClubPostServiceTest | Service | GetByIdAsync_ShouldReturnNull_WhenPostNotFound |
| 15 | ClubPostServiceTest | Service | GetByIdAsync_ShouldReturnMappedDto_WhenFound |
| 16 | ClubPostServiceTest | Service | GetByClubIdAsync_ShouldReturnMappedDtos |
| 17 | ClubPostServiceTest | Service | GetByUserIdAsync_ShouldReturnMappedDtos |
| 18 | ClubPostServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenPostNotFound |
| 19 | ClubPostServiceTest | Service | UpdateAsync_ShouldUpdateStatusToPendingAndEnqueue_WhenImageProvided |
| 20 | ClubPostServiceTest | Service | UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo |
| 21 | ClubPostServiceTest | Service | DeleteAsync_ShouldReturnRepoResult |
| 22 | InterviewServiceTest | Service | CreateScheduleAsync_ShouldCreateScheduleRoomAndAssignments |
| 23 | InterviewServiceTest | Service | UpdateScheduleStatusAsync_ShouldThrowException_WhenStatusInvalid |
| 24 | InterviewServiceTest | Service | UpdateScheduleStatusAsync_ShouldThrowException_WhenConfirmingWrongState |
| 25 | InterviewServiceTest | Service | UpdateScheduleStatusAsync_ShouldUpdateAndSave_WhenValid |
| 26 | InterviewServiceTest | Service | JoinRoomAsync_ShouldThrowException_WhenRoomNotFound |
| 27 | InterviewServiceTest | Service | JoinRoomAsync_ShouldThrowException_WhenRoomClosed |
| 28 | InterviewServiceTest | Service | JoinRoomAsync_ShouldThrowException_WhenRoomFull |
| 29 | InterviewServiceTest | Service | JoinRoomAsync_ShouldAddParticipantAndEvent_WhenValid |
| 30 | InterviewServiceTest | Service | LeaveRoomAsync_ShouldReturnFalse_WhenParticipantNotFound |
| 31 | InterviewServiceTest | Service | LeaveRoomAsync_ShouldUpdateParticipantAndLogEvent_WhenValid |
| 32 | InterviewServiceTest | Service | SubmitFeedbackAsync_ShouldReturnFalse_WhenAssignmentMismatch |
| 33 | InterviewServiceTest | Service | SubmitFeedbackAsync_ShouldUpdateAndReturnTrue_WhenValid |
| 34 | ClubFundControllerTest | Controller | GetFundByClubId_ReturnsOk_WhenFound |
| 35 | ClubFundControllerTest | Controller | GetFundByClubId_ReturnsNotFound_WhenNull |
| 36 | ClubFundControllerTest | Controller | GetTransactions_ReturnsOk_WhenFound |
| 37 | ClubFundControllerTest | Controller | GetTransactions_ReturnsNotFound_WhenNull |
| 38 | ClubFundControllerTest | Controller | AddTransaction_ReturnsCreated_WhenSuccess |
| 39 | ClubFundControllerTest | Controller | AddTransaction_ReturnsBadRequest_WhenFundNotFound |
| 40 | ClubFundControllerTest | Controller | AddTransaction_ReturnsBadRequest_WhenServiceThrows |
| 41 | ClubPostControllerTest | Controller | GetAll_ReturnsOk |
| 42 | ClubPostControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 43 | ClubPostControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 44 | ClubPostControllerTest | Controller | GetByClubId_ReturnsOk |
| 45 | ClubPostControllerTest | Controller | GetByUserId_ReturnsOk |
| 46 | ClubPostControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 47 | ClubPostControllerTest | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 48 | ClubPostControllerTest | Controller | Update_ReturnsOk_WhenSuccess |
| 49 | ClubPostControllerTest | Controller | Update_ReturnsNotFound_WhenNull |
| 50 | ClubPostControllerTest | Controller | Delete_ReturnsOk_WhenFound |
| 51 | ClubPostControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |
| 52 | ClubPostControllerTest | Controller | UploadEditorImage_ReturnsBadRequest_WhenNoFile |
| 53 | ClubPostControllerTest | Controller | UploadEditorImage_ReturnsOk_WhenSuccess |
| 54 | InterviewsControllerTest | Controller | Create_ReturnsCreated_WhenSuccess |
| 55 | InterviewsControllerTest | Controller | Create_ReturnsBadRequest_WhenServiceThrows |
| 56 | InterviewsControllerTest | Controller | GetAll_ReturnsOk |
| 57 | InterviewsControllerTest | Controller | GetById_ReturnsOk_WhenFound |
| 58 | InterviewsControllerTest | Controller | GetById_ReturnsNotFound_WhenNull |
| 59 | InterviewsControllerTest | Controller | Update_ReturnsOk_WhenSuccess |
| 60 | InterviewsControllerTest | Controller | Update_ReturnsNotFound_WhenNull |
| 61 | InterviewsControllerTest | Controller | Update_ReturnsBadRequest_WhenServiceThrows |
| 62 | InterviewsControllerTest | Controller | UpdateStatus_ReturnsOk_WhenSuccess |
| 63 | InterviewsControllerTest | Controller | UpdateStatus_ReturnsNotFound_WhenMissing |
| 64 | InterviewsControllerTest | Controller | UpdateStatus_ReturnsBadRequest_WhenServiceThrows |
| 65 | InterviewsControllerTest | Controller | Delete_ReturnsOk_WhenSuccess |
| 66 | InterviewsControllerTest | Controller | Delete_ReturnsNotFound_WhenMissing |
| 67 | InterviewsControllerTest | Controller | Delete_ReturnsBadRequest_WhenServiceThrows |
| 68 | InterviewsControllerTest | Controller | AssignInterviewers_ReturnsOk_WhenSuccess |
| 69 | InterviewsControllerTest | Controller | AssignInterviewers_ReturnsNotFound_WhenKeyNotFound |
| 70 | InterviewsControllerTest | Controller | GetAssignments_ReturnsOk |
| 71 | InterviewsControllerTest | Controller | RemoveAssignment_ReturnsOk_WhenSuccess |
| 72 | InterviewsControllerTest | Controller | RemoveAssignment_ReturnsNotFound_WhenMissing |
| 73 | InterviewsControllerTest | Controller | ConfirmAssignment_ReturnsOk_WhenSuccess |
| 74 | InterviewsControllerTest | Controller | ConfirmAssignment_ReturnsNotFound_WhenMissing |
| 75 | InterviewsControllerTest | Controller | GetRoom_ReturnsOk_WhenFound |
| 76 | InterviewsControllerTest | Controller | GetRoom_ReturnsNotFound_WhenNull |
| 77 | InterviewsControllerTest | Controller | SubmitFeedback_ReturnsOk_WhenSuccess |
| 78 | InterviewsControllerTest | Controller | SubmitFeedback_ReturnsNotFound_WhenMissing |
| 79 | InterviewsControllerTest | Controller | GetFeedbackSummary_ReturnsOk_WhenFound |
| 80 | InterviewsControllerTest | Controller | GetFeedbackSummary_ReturnsNotFound_WhenNull |
| 81 | RoomsControllerTest | Controller | JoinRoom_ReturnsOk_WhenSuccess |
| 82 | RoomsControllerTest | Controller | JoinRoom_ReturnsNotFound_WhenKeyNotFound |
| 83 | RoomsControllerTest | Controller | JoinRoom_ReturnsBadRequest_WhenServiceThrows |
| 84 | RoomsControllerTest | Controller | LeaveRoom_ReturnsOk_WhenSuccess |
| 85 | RoomsControllerTest | Controller | LeaveRoom_ReturnsNotFound_WhenMissing |
| 86 | RoomsControllerTest | Controller | GetParticipants_ReturnsOk_WhenFound |
| 87 | RoomsControllerTest | Controller | GetParticipants_ReturnsNotFound_WhenKeyNotFound |
| 88 | RoomsControllerTest | Controller | GetEvents_ReturnsOk_WhenFound |
| 89 | RoomsControllerTest | Controller | GetEvents_ReturnsNotFound_WhenKeyNotFound |
| 90 | RoomsControllerTest | Controller | CloseRoom_ReturnsOk_WhenSuccess |
| 91 | RoomsControllerTest | Controller | CloseRoom_ReturnsNotFound_WhenMissing |

**Tổng Zone 5: 91 methods**

---

## Tổng Kết

| Zone | Module | Service | Controller | **Tổng** |
|------|--------|---------|-----------|---------|
| 🟦 Zone 1 | Auth · User · JWT · Email · FileStorage | 50 | 35 | **85** |
| 🟩 Zone 2 | Club · Department · ClubRole · Policy | 38 | 42 | **80** |
| 🟨 Zone 3 | ClubMember · RecruitmentCampaign · Application P1 | 32 | 51 | **83** |
| 🟧 Zone 4 | Event · Attendance · Application P2 | 35 | 46 | **81** |
| 🟥 Zone 5 | ClubFund · ClubPost · Interview · Rooms | 33 | 58 | **91** |
| | **Tổng** | **188** | **232** | **420** |

> `ApplicationControllerTest` chia: Zone 3 (P1 – CRUD & Form, #58–83), Zone 4 (P2 – Batch & Interview, #68–81).
