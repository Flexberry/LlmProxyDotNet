# Frontend v2 Features Implementation

## Overview

This document describes the implementation of v2 features in the frontend, including Rate Limiting, Budget Management, and Team/Org RBAC.

## Implemented Features

### 1. Rate Limiting UI (`/ratelimits`)

**Page**: `frontend/src/app/ratelimits/page.tsx`

**Components**:
- `RateLimitConfigForm.tsx` - Configuration form for rate limits
- List of API keys with selection
- Dialog for configuring individual keys
- Real-time status display

**Features**:
- ✅ API key selection from existing keys
- ✅ Rate limit configuration form with sliders
  - Requests per minute (1-1000)
  - Tokens per minute (100-1,000,000)
  - Requests per day (10-100,000)
- ✅ Toggle to enable/disable rate limiting
- ✅ Visual display of current rate limit status
- ⚠️ Backend API integration pending (endpoints exist but need update endpoint)

**API Functions** (`frontend/src/lib/api.ts`):
```typescript
getRateLimitStatus(apiKeyHash: string): Promise<RateLimitStatus>
```

**Types** (`frontend/src/lib/types.ts`):
```typescript
interface RateLimitConfig {
  requestsPerMinute: number;
  tokensPerMinute: number;
  requestsPerDay: number;
  maxDailyCost?: number;
}

interface RateLimitStatus {
  requestsThisMinute: number;
  requestsThisDay: number;
  tokensThisMinute: number;
  isRateLimited: boolean;
  resetAt?: string;
}
```

### 2. Budget Management UI (`/ratelimits`)

**Page**: Same as Rate Limiting (`/ratelimits`)

**Components**:
- `BudgetConfigForm.tsx` - Configuration form for budgets
- Budget status display in dialog

**Features**:
- ✅ Budget configuration form
  - Budget amount in dollars
  - Limit action selection (warn/block)
- ✅ Toggle to enable/disable budget management
- ✅ Visual display of current budget status
  - Total budget
  - Current spending
  - Remaining budget
  - Percentage used
- ✅ Backend API integration (functional)

**API Functions**:
```typescript
getBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<Budget | null>
setBudget(entityType: 'ApiKey' | 'Team', entityId: string, request: SetBudgetRequest): Promise<Budget>
checkBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<BudgetCheckResult>
updateSpending(entityType: 'ApiKey' | 'Team', entityId: string, request: UpdateSpendingRequest): Promise<BudgetCheckResult>
```

**Types**:
```typescript
interface Budget {
  id: string;
  entityId: string;
  entityType: 'ApiKey' | 'Team';
  budgetAmount: number;
  currentSpending: number;
  limitAction: 'warn' | 'block';
  periodStart?: string;
  periodEnd?: string;
  createdAt: string;
  updatedAt: string;
}

interface BudgetCheckResult {
  budgetAmount: number;
  currentSpending: number;
  remainingBudget: number;
  shouldBlock: boolean;
  percentageUsed: number;
}

interface SetBudgetRequest {
  budgetAmount: number;
  limitAction: 'warn' | 'block';
  periodEnd?: string;
}
```

### 3. Team/Org RBAC UI (`/teams`)

**Page**: `frontend/src/app/teams/page.tsx`

**Components**:
- Team list with cards
- Team creation dialog
- Team details dialog
- Member management UI

**Features**:
- ✅ Team list view with cards
- ✅ Team creation dialog
  - Name input
  - Description input
- ✅ Team details dialog
  - Team information display
  - Members list (pending backend)
  - Member roles display
- ⚠️ Add/remove member functionality (UI ready, backend integration pending)
- ✅ Delete team functionality

**API Functions**:
```typescript
createTeam(request: CreateTeamRequest): Promise<Team>
getUserTeams(): Promise<Team[]>
getTeam(teamId: string): Promise<Team>
deleteTeam(teamId: string): Promise<void>
addTeamMember(teamId: string, request: AddTeamMemberRequest): Promise<TeamMember>
removeTeamMember(teamId: string, userId: string): Promise<void>
getUserRole(teamId: string, userId: string): Promise<TeamMember>
```

**Types**:
```typescript
type TeamRole = 'Owner' | 'Admin' | 'Member' | 'Viewer';

interface Team {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

interface TeamMember {
  id: string;
  teamId: string;
  userId: string;
  role: TeamRole;
  allowedModels?: string[];
  createdAt: string;
}

interface CreateTeamRequest {
  name: string;
  description?: string;
}

interface AddTeamMemberRequest {
  userId: string;
  role: TeamRole;
  allowedModels?: string[];
}
```

## Navigation

New menu items added to sidebar (`frontend/src/components/layout/Sidebar.tsx`):

- **Rate Limit & Budget** - `/ratelimits` (Shield icon)
- **Команды** - `/teams` (Users icon)

## UI Components

### New Components Created

1. **RateLimitConfigForm.tsx**
   - Path: `frontend/src/app/keys/components/RateLimitConfigForm.tsx`
   - Features sliders for rate limit configuration
   - Toggle for enabling/disabling

2. **BudgetConfigForm.tsx**
   - Path: `frontend/src/app/keys/components/BudgetConfigForm.tsx`
   - Budget amount input
   - Limit action radio buttons
   - Usage explanation

3. **Select Component**
   - Path: `frontend/src/components/ui/select.tsx`
   - Radix UI based select component
   - Used in team member role selection

4. **DialogFooter Component**
   - Path: `frontend/src/components/ui/dialog.tsx` (added)
   - Footer layout for dialogs
   - Responsive button positioning

## Dependencies Added

```json
{
  "@radix-ui/react-select": "^2.1.0"
}
```

## Integration Points

### Backend Endpoints Used

**Rate Limiting**:
- `GET /admin/ratelimits/{hash}` - Get rate limit status
- `POST /admin/ratelimits/{hash}/config` - Update rate limit config (pending)
- `POST /admin/ratelimits/{hash}/reset` - Reset rate limits

**Budget Management**:
- `GET /admin/budgets/{entityType}/{entityId}` - Get budget
- `POST /admin/budgets/{entityType}/{entityId}` - Set/update budget
- `GET /admin/budgets/{entityType}/{entityId}/check` - Check budget status
- `POST /admin/budgets/{entityType}/{entityId}/spending` - Update spending

**Team Management**:
- `POST /admin/teams` - Create team
- `GET /admin/teams` - Get user's teams
- `GET /admin/teams/{id}` - Get team details
- `DELETE /admin/teams/{id}` - Delete team
- `POST /admin/teams/{id}/members` - Add member
- `DELETE /admin/teams/{id}/members/{userId}` - Remove member
- `GET /admin/teams/{id}/members/{userId}/role` - Get user role

## Future Work

### High Priority

1. **Rate Limit Config Update API**
   - Backend endpoint to update rate limit configuration
   - Frontend integration in `RateLimitConfigForm`

2. **Enhanced Budget Visualization**
   - Charts for spending over time
   - Budget alerts and notifications

### Medium Priority

3. **Team Member Management**
   - Add member dialog with email/user ID input
   - Role selection dropdown
   - Remove member confirmation
   - Member list with avatars

4. **Team API Key Assignment**
   - Create API keys for teams
   - Assign keys to teams on creation

### Low Priority

5. **Webhook Configuration UI**
   - Webhook URL input
   - Event selection (checkboxes)
   - Test webhook button

6. **Response Cache Management**
   - View cache statistics
   - Clear cache by model
   - Cache configuration

## Testing

### Manual Testing Checklist

- [ ] Navigate to `/ratelimits` page
- [ ] Select an API key from the list
- [ ] Configure rate limits using sliders
- [ ] Toggle rate limiting on/off
- [ ] Configure budget amount
- [ ] Select limit action (warn/block)
- [ ] View budget status display
- [ ] Navigate to `/teams` page
- [ ] Create a new team
- [ ] View team details
- [ ] Test team deletion

### API Testing

Use the backend API directly for full functionality testing:
```bash
# Rate Limiting
curl http://localhost:4000/admin/ratelimits/{hash}

# Budget
curl http://localhost:4000/admin/budgets/ApiKey/{hash}

# Teams
curl http://localhost:4000/admin/teams
```

See `../V2_TESTING_GUIDE.md` for detailed API testing instructions.

## File Structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── ratelimits/
│   │   │   └── page.tsx              # Rate Limit & Budget page
│   │   ├── teams/
│   │   │   └── page.tsx              # Team management page
│   │   └── keys/
│   │       └── components/
│   │           ├── RateLimitConfigForm.tsx
│   │           └── BudgetConfigForm.tsx
│   ├── components/
│   │   ├── layout/
│   │   │   └── Sidebar.tsx           # Updated with new menu items
│   │   └── ui/
│   │       ├── select.tsx            # New component
│   │       └── dialog.tsx            # Added DialogFooter
│   └── lib/
│       ├── api.ts                    # Added v2 API functions
│       └── types.ts                  # Added v2 type definitions
└── README_V2.md                      # This file
```

## Version

**v2.0.0** - 2026-06-05
- Initial implementation of v2 frontend features
- Rate Limiting UI
- Budget Management UI
- Team Management UI
- Navigation updates
- Type definitions
- API function implementations
