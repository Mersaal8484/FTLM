import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import Button from './Button';
import { Plus } from 'lucide-react';

describe('UI Button Component', () => {
  test('renders button with children text', () => {
    render(<Button>Click Me</Button>);
    expect(screen.getByRole('button', { name: /Click Me/i })).toBeInTheDocument();
  });

  test('applies variant class and size inline style correctly', () => {
    const { container } = render(
      <Button variant="danger" size="sm">
        Delete
      </Button>
    );
    const btn = container.querySelector('button');
    // variant maps to a CSS class
    expect(btn).toHaveClass('btn-danger');
    // size maps to inline style (not a CSS class)
    expect(btn.style.height).toBe('30px');
  });

  test('handles click events when enabled', () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Submit</Button>);
    fireEvent.click(screen.getByRole('button', { name: /Submit/i }));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  test('disables button and displays spinner when loading is true', () => {
    const handleClick = vi.fn();
    const { container } = render(
      <Button loading onClick={handleClick}>
        Save
      </Button>
    );
    const btn = screen.getByRole('button');
    expect(btn).toBeDisabled();
    expect(container.querySelector('.ft-spinner')).toBeInTheDocument();

    fireEvent.click(btn);
    expect(handleClick).not.toHaveBeenCalled();
  });

  test('renders icon alongside children', () => {
    render(<Button icon={Plus}>Add Item</Button>);
    expect(screen.getByRole('button', { name: /Add Item/i })).toBeInTheDocument();
  });
});
