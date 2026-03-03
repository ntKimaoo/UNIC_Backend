-- Chạy script này MỘT LẦN trên database UNIC khi gặp lỗi "There is already an object named 'ClubRoles'"
-- Script đánh dấu migration 20260224040751_initalCreateDB đã được apply (vì schema thực tế đã có từ migration cũ)
USE [UNIC];
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260224040751_initalCreateDB')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260224040751_initalCreateDB', N'9.0.9');
END
GO
