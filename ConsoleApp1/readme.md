Method execution order in filters:

OnCreating
OnStateElection
OnStateUnapplied - state: ``
OnStateApplied - state: Enqueued
OnCreated
OnStateElection - CandidateState: Processing
OnStateUnapplied
OnStateApplied - processing
OnPerforming
OnPerformed
OnStateeElection - succeeded
OnStateUnapplied
OnStateApplied
