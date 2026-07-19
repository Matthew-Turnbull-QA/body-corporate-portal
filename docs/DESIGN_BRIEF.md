# Body Corporate Management Portal — Design Brief

## Purpose
Create a clean, professional internal admin portal UI for body corporate staff to manage properties and users. The design should support a future-facing dashboard while keeping the current scope lean and highly usable.

## Target users
- Body corporate administrators
- Property managers
- Support staff who need to manage user accounts and maintain property records

## Core objectives
- Enable fast login with Google SSO
- Provide clear access to property and user management
- Support role-based navigation and administrator-only user controls
- Deliver a polished, accessible admin UI that scales to future modules

## Pages and flows
### 1. Login page
- Google sign-in only
- Simple hero section with brief portal purpose
- Error state messaging for login failures
- Mobile-friendly centered layout

### 2. App shell / header
- Top-level header with portal title
- Primary navigation item: `Properties`
- Secondary navigation item: `Users` visible only to administrators
- Signed-in user badge showing display name and role
- Sign out button

### 3. Home / dashboard
- Lightweight landing page after login
- Show current user email and role
- Placeholder section for future job/task management

### 4. Properties list
- Table columns: Name, Address, Suburb, State, Postcode
- Primary action: Add property
- Inline add form or side panel/modal for new property input
- Clean table rows with row striping or subtle dividers

### 5. Users list (Administrator-only)
- Table columns: Email, Name, Role, Status, Last login
- Actions: Edit, Enable / Disable
- Add user dialog or panel
- Disable self action should be prevented and communicated clearly
- Account status chips or badges for enabled/disabled state

## Visual style direction
- Corporate and trust-worthy: calm blues, grays, neutral tones, with a strong accent color
- Clean spacing and strong section hierarchy
- Modern dashboard feel without unnecessary complexity
- Subtle shadows or surface layers for cards and tables
- Accessible typography, color contrast, and focus states

## UI patterns and components
- Primary and secondary buttons
- Data table with responsive layout and row actions
- Form fields with labels and inline validation states
- Modal/dialog for add/edit flows
- Role-based navigation visibility
- Status chips/badges for user account state

## Accessibility and usability
- Ensure keyboard focus states are visible
- Use ARIA-friendly alerts for login and load errors
- Clear button labels and action affordances
- Responsive layout for laptop and desktop screens

## Notes for designers
- Keep the first release focused on property and user management only
- Design for clear hierarchy: login → app shell → list pages
- Support future expansion of dashboard and additional management modules
- Avoid overly decorative visuals; prioritize clarity and quick task completion

## Delivery expectations
Provide:
- Login screen design
- App shell / header/nav layout
- Properties page design with add-property interaction
- Users page design with edit and enable/disable interactions
- Dashboard/home placeholder design
