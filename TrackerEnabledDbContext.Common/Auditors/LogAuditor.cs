﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Dynamic;
using System.Linq;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Extensions;
using TrackerEnabledDbContext.Common.Interfaces;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Common.Auditors
{
    internal class LogAuditor : IDisposable
    {
        private readonly DbEntityEntry _dbEntry;
        private DbPropertyValues dbEntryDbValues;

        internal LogAuditor(DbEntityEntry dbEntry)
        {
            _dbEntry = dbEntry;
        }

        public void Dispose()
        {
        }

        internal AuditLog CreateLogRecord(object userName, EventType eventType, ITrackerContext context, ExpandoObject metadata)
        {
            Type entityType = _dbEntry.Entity.GetType().GetEntityType();

            if (!EntityTrackingConfiguration.IsTrackingEnabled(entityType))
            {
                return null;
            }

            DateTime changeTime = DateTime.UtcNow;

            //todo: make this a static class
            var mapping = new DbMapping(context, entityType);
            List<PropertyConfiguerationKey> keyNames = mapping.PrimaryKeys().ToList();

            var newlog = new AuditLog
            {
                UserName = userName?.ToString(),
                EventDateUTC = changeTime,
                EventType = eventType,
                TypeFullName = entityType.FullName,
                RecordId = GetPrimaryKeyValuesOf(_dbEntry, keyNames).ToString()
            };

            var logMetadata = metadata
                .Where(x => x.Value != null)
                .Select(m => new LogMetadata
                {
                    AuditLog = newlog,
                    Key = m.Key,
                    Value = m.Value?.ToString()
                })
            .ToList();

            newlog.Metadata = logMetadata;

            var detailsAuditor = GetDetailsAuditor(eventType, newlog);

            newlog.LogDetails = detailsAuditor.CreateLogDetails().ToList();

            if (newlog.LogDetails.Any())
                return newlog;
            else
                return null;
        }

        private ChangeLogDetailsAuditor GetDetailsAuditor(EventType eventType, AuditLog newlog)
        {
            switch (eventType)
            {
                case EventType.Added:
                    return new AdditionLogDetailsAuditor(_dbEntry, newlog, dbEntryDbValues);

                case EventType.Deleted:
                    return new DeletetionLogDetailsAuditor(_dbEntry, newlog, dbEntryDbValues);

                case EventType.Modified:
                    return new ChangeLogDetailsAuditor(_dbEntry, newlog, dbEntryDbValues);

                case EventType.SoftDeleted:
                    return new SoftDeletedLogDetailsAuditor(_dbEntry, newlog, dbEntryDbValues);

                case EventType.UnDeleted:
                    return new UnDeletedLogDetailsAudotor(_dbEntry, newlog, dbEntryDbValues);

                default:
                    return null;
            }
        }

        private object GetPrimaryKeyValuesOf(
            DbEntityEntry dbEntry,
            List<PropertyConfiguerationKey> properties)
        {
            if (properties.Count == 1)
            {
                return OriginalValue(properties.First().PropertyName);
            }
            if (properties.Count > 1)
            {
                string output = "[";

                output += string.Join(",",
                    properties.Select(colName => OriginalValue(colName.PropertyName)));

                output += "]";
                return output;
            }
            throw new KeyNotFoundException("key not found for " + dbEntry.Entity.GetType().FullName);
        }

        protected virtual object OriginalValue(string propertyName)
        {
            object originalValue = null;

            if (GlobalTrackingConfig.DisconnectedContext)
            {
                if (dbEntryDbValues == null)
                    dbEntryDbValues = _dbEntry.GetDatabaseValues();

                originalValue = dbEntryDbValues.GetValue<object>(propertyName);
            }
            else
            {
                originalValue = _dbEntry.Property(propertyName).OriginalValue;
            }

            return originalValue;
        }
    }
}