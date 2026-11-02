import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import FormField, { Input } from './FormField';

describe('UI FormField & Input Components', () => {
  test('renders FormField with label and required star', () => {
    render(
      <FormField label="Username" required>
        <Input placeholder="Enter username" />
      </FormField>
    );
    expect(screen.getByText('Username')).toBeInTheDocument();
    expect(screen.getByText('*')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter username')).toBeInTheDocument();
  });

  test('displays error message when error prop is provided', () => {
    render(
      <FormField label="Email" error="Invalid email address">
        <Input error="Invalid email address" />
      </FormField>
    );
    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
  });

  test('displays helper text (hint) when provided and no error exists', () => {
    render(
      <FormField label="Password" hint="Minimum 8 characters">
        <Input type="password" />
      </FormField>
    );
    expect(screen.getByText('Minimum 8 characters')).toBeInTheDocument();
  });
});
