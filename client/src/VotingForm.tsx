import React from 'react';

interface VotingFormProps {
  storyName: string;
  cardValues: string;
  timerSeconds: string;
  onStoryNameChange: (value: string) => void;
  onCardValuesChange: (value: string) => void;
  onTimerSecondsChange: (value: string) => void;
  onSubmit: () => void;
  submitDisabled: boolean;
  submitLabel?: string;
  title?: string;
  idPrefix?: string;
}

export default function VotingForm({
  storyName,
  cardValues,
  timerSeconds,
  onStoryNameChange,
  onCardValuesChange,
  onTimerSecondsChange,
  onSubmit,
  submitDisabled,
  submitLabel = 'Start Voting',
  title = 'Start a New Voting Round',
  idPrefix = ''
}: VotingFormProps) {
  return (
    <div>
      {title && <h3>{title}</h3>}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor={`${idPrefix}story-name`}>Story Name:</label>
        <input
          id={`${idPrefix}story-name`}
          value={storyName}
          onChange={e => onStoryNameChange(e.target.value)}
          style={{ width: '100%', padding: '0.5rem' }}
          placeholder="Enter story name"
        />
      </div>
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor={`${idPrefix}card-values`}>Card Values (comma separated):</label>
        <input
          id={`${idPrefix}card-values`}
          value={cardValues}
          onChange={e => onCardValuesChange(e.target.value)}
          style={{ width: '100%', padding: '0.5rem' }}
          placeholder="e.g., 1,2,3,5,8,13,21,?"
        />
      </div>
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor={`${idPrefix}timer-seconds`}>Timer (seconds, clear to disable):</label>
        <input
          id={`${idPrefix}timer-seconds`}
          type="number"
          value={timerSeconds}
          onChange={e => onTimerSecondsChange(e.target.value)}
          style={{ width: '100%', padding: '0.5rem' }}
          placeholder="e.g., 60"
        />
      </div>
      <div style={{ display: 'flex', justifyContent: 'center', marginTop: '1.5rem' }}>
        <button 
          onClick={onSubmit} 
          disabled={submitDisabled}
          style={{
            padding: '0.8rem 2rem',
            fontSize: '1.1rem',
            fontWeight: 'bold',
            backgroundColor: submitDisabled ? '#ccc' : '#4CAF50',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: submitDisabled ? 'not-allowed' : 'pointer',
            transition: 'background-color 0.2s',
            minWidth: '200px'
          }}
          onMouseOver={e => !submitDisabled && (e.currentTarget.style.backgroundColor = '#45a049')}
          onMouseOut={e => !submitDisabled && (e.currentTarget.style.backgroundColor = '#4CAF50')}
        >
          {submitLabel}
        </button>
      </div>
    </div>
  );
}
